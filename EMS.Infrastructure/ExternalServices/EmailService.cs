using EMS.Core.Interfaces.ExternalServices;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace EMS.Core.Services.ExternalServices;

// 🔥 Brevo ki REST API use karta hai (HTTPS port 443) — SMTP (port 587) NAHI,
// kyunki Render free tier SMTP ports block karta hai (25/465/587).
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    // 🔥 NEW: `oldOtpCode` Parameter Add किया गया है (Optional — Default = null)
    public async Task<bool> SendOtpEmailAsync(string toEmail, string newOtpCode, string? oldOtpCode)
    {
        var emailSettings = _config.GetSection("EmailSettings");
        var senderEmail = emailSettings["SenderEmail"] ?? throw new InvalidOperationException("EmailSetting : SenderEmail is Missing.");
        var senderName = emailSettings["SenderName"] ?? "A_EMS Team";
        var apiKey = emailSettings["BrevoApiKey"] ?? throw new InvalidOperationException("EmailSetting : BrevoApiKey is Missing.");

        string subject;
        string htmlContent;

        if (!string.IsNullOrEmpty(oldOtpCode))
        {
            // Resend Case
            subject = "Your New OTP Code (Resent)";
            htmlContent = $@"
        <p>You have requested another OTP.</p>
        <p>The previous code <b>{oldOtpCode}</b> is now invalid.</p>
        <p>Your new OTP code is: <b>{newOtpCode}</b></p>
        <p>This code will expire in 5 minutes.</p>";
        }
        else
        {
            subject = "Your EMS OTP Code";
            htmlContent = $@"
        <p>Your OTP code is: <b>{newOtpCode}</b></p>
        <p>This code will expire in 5 minutes.</p>";
        }

        var payload = new
        {
            sender = new { email = senderEmail, name = senderName },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent
        };

        var json = JsonSerializer.Serialize(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to send OTP email (Status: {response.StatusCode}). {errorBody}");

        }
    }
}