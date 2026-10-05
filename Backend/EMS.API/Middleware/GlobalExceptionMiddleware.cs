using System.Net;
using System.Text.Json;
using EMS.Core.Common;
using EMS.Core.Exceptions;

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
        // 🔍 Exception के Type के हिसाब से Status Code Decide करो
        var (statusCode, message) = exception switch
        {
            // 🔥 Custom Exceptions (400, 404, 409) को पहचानो
            BadRequestException badRequest => ((int)HttpStatusCode.BadRequest, badRequest.Message),
            NotFoundException notFound => ((int)HttpStatusCode.NotFound, notFound.Message),
            ConflictException conflict => ((int)HttpStatusCode.Conflict, conflict.Message),
            ExternalServiceException externalService => ((int)HttpStatusCode.ServiceUnavailable, externalService.Message),

            // 🔥 Unauthorized (401) — Optional
            UnauthorizedAccessException unauthorized => ((int)HttpStatusCode.Unauthorized, unauthorized.Message),

            // 🔥 बाकी सारी Exceptions (जो हमने नहीं पकड़ी) → 500 Internal Server Error
            _ => ((int)HttpStatusCode.InternalServerError, _env.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Please try again later.")
        };

        // 🔹 Response को JSON Format में Return करो    
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        // ✅ ApiResponse Wrapper में Response भेजो (जो Client (Angular) को Standard JSON मिले)
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        };

        // 🔥 सिर्फ Development Environment में Stack Trace भेजो (Production में नहीं!)
        // if (_env.IsDevelopment())
        // {
        //     //  response.StackTrace = exception.StackTrace;
        // }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(jsonResponse);
    }
}