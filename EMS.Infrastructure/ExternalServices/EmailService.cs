using EMS.Core.Exceptions;
using EMS.Core.Interfaces.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EMS.Infrastructure.ExternalServices;

// 🔥 Brevo ki REST API use karta hai (HTTPS port 443) — SMTP (port 587) NAHI,
// kyunki Render free tier SMTP ports block karta hai (25/465/587).
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, HttpClient httpClient, ILogger<EmailService> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
    }

    // 🔥 NEW: `oldOtpCode` Parameter Add किया गया है (Optional — Default = null)
    public async Task<bool> SendOtpEmailAsync(string toEmail, string newOtpCode, string? oldOtpCode = null)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"]
            ?? throw new InvalidOperationException("EmailSettings:SenderEmail is missing.");
        var senderName = emailSettings["SenderName"] ?? "EMS Team";
        var apiKey = emailSettings["BrevoApiKey"]
            ?? throw new InvalidOperationException("EmailSettings:BrevoApiKey is missing.");

        string subject, body;
        if (!string.IsNullOrEmpty(oldOtpCode))
        {
            subject = "🔐 Your New OTP Code (Resent)";
            body = $@"
            <p><strong>⚠️ Important:</strong> Your previous OTP (<b>{oldOtpCode}</b>) is no longer valid.</p>
            <p>Your new OTP code is: <b style='font-size:24px;'>{newOtpCode}</b></p>
            <p>This code will expire in <b>5 minutes</b>.</p>
            <p>If you didn't request this, please ignore this email.</p>";
        }
        else
        {
            subject = "🔐 Your EMS OTP Code";
            body = $@"
            <p>Your OTP code is: <b style='font-size:24px;'>{newOtpCode}</b></p>
            <p>This code will expire in <b>5 minutes</b>.</p>
            <p>If you didn't request this, please ignore this email.</p>";
        }

        var payload = new
        {
            sender = new { email = senderEmail, name = senderName },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = body
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Brevo API failed: {StatusCode} - {ErrorBody}", response.StatusCode, errorBody);  // agar ILogger inject kiya ho to
            throw new ExternalServiceException("Unable to send OTP email at the moment. Please try again shortly.");
        }
        return true;


    }
}