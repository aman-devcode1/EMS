using System.Text;
using EMS.API.Middleware;
using EMS.Core.Interfaces;
using EMS.Infrastructure.Data;
using EMS.Infrastructure.Repositories;
using EMS.Services.Mapping;
using EMS.Services.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database Context को Register करें (Local vs Live के हिसाब से)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemory");

if (useInMemory)
{
    // Live Demo / Testing के लिए In-Memory Database
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("EMSInMemoryDb"));
    Console.WriteLine("Using In-Memory Database");
}
else
{
    // Local Development के लिए SQL Server
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
    Console.WriteLine("Using SQL Server Database");
}

// Database Context (AppDbContext) को Register करें
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers को Enable करें (API Endpoints के लिए)
builder.Services.AddControllers();

// AutoMapper Register करें (यह Line पहले से होनी चाहिए)
builder.Services.AddAutoMapper(typeof(AutoMapperProfile)); // ✅

// Repositories और Services Register करें
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// JWT AUTHENTICATION
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is missing."));

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

// Swagger/OpenAPI (अगर चाहें तो)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>(); // Custom Exception Handling Middleware को Use करें

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.swaggerEndpoint("/swagger/v1/swagger.json", "EMS API V1");
    });

    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();  // 👈 Authentication को Use करें
app.UseAuthorization();
app.MapControllers();  // 👈 Controllers को Map करें

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.Run();

