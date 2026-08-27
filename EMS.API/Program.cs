using System.Text;
using EMS.API.Middleware;
using EMS.Core.Interfaces;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Services;
using EMS.Infrastructure.Repositories;
using AutoMapper;
using EMS.Services.Mapping;
using EMS.Services.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.HttpOverrides;

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
// AutoMapper - Manually Configure (Static API)
// ============================================================
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AutoMapperProfile>();
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddHttpClient<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// ============================================================
// JWT AUTHENTICATION
// ============================================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ??
    throw new InvalidOperationException("JWT Secret Key is missing."));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(secretKey)
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
app.UseMiddleware<GlobalExceptionMiddleware>();

// ✅ Swagger - Production में भी चालू (ताकि Live Demo चले)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EMS API V1");
});

if (!app.Environment.IsDevelopment()) { app.UseHttpsRedirection(); }
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();