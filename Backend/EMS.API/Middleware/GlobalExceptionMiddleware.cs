using System.Net;
using System.Text.Json;
using EMS.Core.Common;
using EMS.Core.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EMS.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            // Hamare apne exceptions: message seedha user ko
            BadRequestException ex => ((int)HttpStatusCode.BadRequest, ex.Message),
            NotFoundException ex => ((int)HttpStatusCode.NotFound, ex.Message),
            ConflictException ex => ((int)HttpStatusCode.Conflict, ex.Message),
            ExternalServiceException ex => ((int)HttpStatusCode.ServiceUnavailable, ex.Message),
            UnauthorizedAccessException ex => ((int)HttpStatusCode.Unauthorized, ex.Message),

            // Database ke pehchane hue karan
            DbUpdateConcurrencyException => ((int)HttpStatusCode.Conflict,
                "This record was changed by someone else while you were editing. Please reload and try again."),
            DbUpdateException ex when IsUniqueViolation(ex) => ((int)HttpStatusCode.Conflict, BuildDuplicateMessage(ex)),
            DbUpdateException => ((int)HttpStatusCode.InternalServerError,
                "The data could not be saved to the database. Please check the values and try again."),
            SqlException => ((int)HttpStatusCode.ServiceUnavailable,
                "The database is not reachable right now. Please try again in a moment."),

            // Request / timeout ke karan
            BadHttpRequestException or JsonException => ((int)HttpStatusCode.BadRequest,
                "The request data is not in a valid format. Please check the fields and try again."),
            TimeoutException => ((int)HttpStatusCode.GatewayTimeout,
                "The operation took too long and timed out. Please try again."),

            // Baaki sab: Development mein asli message, Production mein reference ID
            _ => ((int)HttpStatusCode.InternalServerError, _env.IsDevelopment()
                ? exception.Message
                : $"An unexpected server error occurred. If it keeps happening, share this reference with support: {context.TraceIdentifier}")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);

    private static string BuildDuplicateMessage(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? string.Empty;
        if (text.Contains("Email", StringComparison.OrdinalIgnoreCase))
            return "This email is already registered with another account. Please use a different email.";
        if (text.Contains("Phone", StringComparison.OrdinalIgnoreCase))
            return "This phone number is already registered with another account. Please use a different number.";
        return "A record with the same unique value already exists. Please use different details.";
    }
}