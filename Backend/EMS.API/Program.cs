using System.Text;
using EMS.API.Middleware;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;
using EMS.Services.Mapping;
using EMS.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;
using EMS.Core.Interfaces.IRepositories;
using EMS.Core.Interfaces.IServices;
using EMS.Core.Interfaces.ExternalServices;
using EMS.Infrastructure.ExternalServices;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using System.Net.Sockets;
using EMS.Core.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto; });

// ============================================================
// DATABASE CONTEXT (In-Memory vs SQL Server)
// ============================================================
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemory");

if (useInMemory)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("EMSInMemoryDb"));
    Console.WriteLine("🚀 Using In-Memory Database");
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    Console.WriteLine("💾 Using SQL Server Database");
}

// ============================================================
// SERVICES REGISTRATION
// ============================================================
builder.Services.AddControllers();

// ============================================================
// CORS — Origins from configuration (environment-aware)
// ============================================================
var allowedOrigins = builder.Configuration
    .GetSection("CorsSettings:AllowedOrigins")
    .Get<string[]>() 
    ?? new[] { "http://localhost:4200" };   // fallback for safety

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();   // for Cookie purpose
    });
});

// ============================================================
// AutoMapper - Manually Configure (Static API)
// ============================================================
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AutoMapperProfile>();
});

// REPOSITORIES
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

// SERVICES
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddSingleton<BackgroundEmailQueue>();
builder.Services.AddScoped<IDesignationService, DesignationService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();

builder.Services.AddSingleton<IBackgroundEmailQueue>(sp => sp.GetRequiredService<BackgroundEmailQueue>());
builder.Services.AddSingleton<IEmailStatusStore, EmailStatusStore>();

builder.Services.AddHostedService<EmailQueueProcessor>();
builder.Services.AddHostedService<EMS.API.Services.TokenCleanupService>();

// HEALTH CHECKS
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>(
    name: "database",
    failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
    tags: new[] { "db", "sql", "critical" }
);

// Rate Limiter
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth-strict", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("auth-refresh", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 10;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// HTTP CLIENT (Brevo/SendGrid के लिए)
builder.Services.AddHttpClient<IEmailService, EmailService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    // Sirf IPv4 use karna hai kyuki IPv6 hang ho jata hai.
    ConnectCallback = async (context, cancellationToken) =>
    {
        var entry = await Dns.GetHostEntryAsync(
            context.DnsEndPoint.Host, AddressFamily.InterNetwork, cancellationToken
        );

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(entry.AddressList, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
});

builder.Services.AddMemoryCache();

// ============================================================
// JWT AUTHENTICATION
// ============================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings section is missing.");

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

builder.Services.Configure<OtpSettings>(
    builder.Configuration.GetSection("OtpSettings"));

builder.Services.PostConfigure<OtpSettings>(
    o => o.LogOtpToConsole &= builder.Environment.IsDevelopment()); // Only log OTP in development mode

var secretKey = Encoding.UTF8.GetBytes(jwtSettings.SecretKey
    ?? throw new InvalidOperationException("JWT Secret Key is missing."));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),

            NameClaimType = ClaimTypes.Name,
            RoleClaimType = "role",

            ClockSkew = TimeSpan.FromSeconds(jwtSettings.ClockSkewSeconds)
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// SWAGGER (Production में भी चालू - Demo के लिए)
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================
// APP PIPELINE
// ============================================================
var app = builder.Build();

app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";
    await next();
});
app.UseMiddleware<GlobalExceptionMiddleware>();

// ✅🔥 CRITICAL: Order matters — DefaultFiles pehle, StaticFiles baad me (Swagger/CORS se pehle)
var frontendFileProvider = new PhysicalFileProvider(
    Path.Combine(app.Environment.ContentRootPath, "wwwroot", "browser"));

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = frontendFileProvider
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = frontendFileProvider
});

// ✅ Swagger - यह अब `/` को ओवरराइड नहीं करेगा
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMS API V1");
});

app.UseCors("AllowAngularApp");
app.UseRateLimiter();

if (!app.Environment.IsDevelopment()) { app.UseHttpsRedirection(); }

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds + "ms",
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds + "ms",
                description = e.Value.Description
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = frontendFileProvider
});

app.Run();