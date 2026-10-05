using EMS.Core.Configuration;
using EMS.Core.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EMS.Infrastructure.ExternalServices;

// Email provider ki HTTPS REST API use karta hai (SMTP ports nahi, kyunki kuch hosts unhe block karte hain).
// Provider ka URL, API key aur header ka naam sab "EmailSettings" config se aate hain.
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly OtpSettings _otpSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, HttpClient httpClient, IOptions<OtpSettings> otpSettings, ILogger<EmailService> logger)
    {
        _config = config;
        _httpClient = httpClient;
        _otpSettings = otpSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string newOtpCode, bool isResend, OtpPurpose purpose = OtpPurpose.Registration)
    {
        var purposeLabel = purpose switch
        {
            OtpPurpose.PasswordReset => "password reset",
            OtpPurpose.Login => "login",
            _ => "registration",
        };

        var disclaimer = purpose == OtpPurpose.PasswordReset
            ? "If you didn't request a password reset, please ignore this email — your password will remain unchanged."
            : "If you didn't request this, please ignore this email.";

        string subject, body;
        if (isResend)
        {
            // 🔥 Purane code ki VALUE kahin bhi nahi likhi — sirf itna bataya jaata hai
            // ki wo ab invalid hai. Ek expose ho chuka secret dobara dikhane ka koi
            // fayda nahi, sirf leak-surface badhta hai.
            subject = purpose == OtpPurpose.PasswordReset
                ? "🔐 Your New Password Reset Code (Resent)"
                : "🔐 Your New OTP Code (Resent)";
            body = $@"
            <p><strong>⚠️ Important:</strong> Your previous code is no longer valid.</p>
            <p>Your new {purposeLabel} code is: <b style='font-size:24px;'>{newOtpCode}</b></p>
            <p>This code will expire in <b>{_otpSettings.ExpiryMinutes} minutes</b>.</p>
            <p>{disclaimer}</p>";
        }
        else
        {
            subject = purpose == OtpPurpose.PasswordReset
                ? "🔐 Your EMS Password Reset Code"
                : "🔐 Your EMS OTP Code";
            body = $@"
            <p>Your {purposeLabel} code is: <b style='font-size:24px;'>{newOtpCode}</b></p>
            <p>This code will expire in <b>{_otpSettings.ExpiryMinutes} minutes</b>.</p>
            <p>{disclaimer}</p>";
        }

        // Fail hone par ExternalServiceException throw hota hai (queue processor retry karta hai)
        await SendAsync(toEmail, subject, body);
        return true;
    }

    // 🔥 Password reset hone ke baad best-effort notification — is method ka fail hona
    // reset operation ko fail NAHI karta (AuthService isse try/catch mein wrap karta hai),
    // isliye yahan ExternalServiceException throw nahi karte, bas false return karte hain.
    public async Task<bool> SendPasswordChangedEmailAsync(string toEmail)
    {
        var subject = "✅ Your EMS Password Was Changed";

        var body = @"
        <p>This is confirmation that your EMS account password just changed.</p>
        <p>For your security, you have been signed out of <b>all devices</b> - please login again with your new password.</p>
        <p>If you did not make this change, please contact your administrator immediately.</p>";

        try
        {
            await SendAsync(toEmail, subject, body);
            return true;
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogWarning(ex, "Password-changed email to {Email} could not be sent.", toEmail);
            return false;   // best-effort — exception nahi fenkte
        }
    }

    // ============================================================
    // PRIVATE: config padhna + request bhejna (ek hi jagah)
    // ============================================================
    private (string ApiUrl, string ApiKey, string ApiKeyHeader, string SenderEmail, string SenderName) GetSettings()
    {
        var section = _config.GetSection("EmailSettings");

        string Required(string key) =>
            section[key] is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException($"EmailSettings:{key} is missing.");

        return (Required("ApiUrl"), Required("ApiKey"), Required("ApiKeyHeader"),
                Required("SenderEmail"), Required("SenderName"));
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var settings = GetSettings();

        var payload = new
        {
            sender = new { email = settings.SenderEmail, name = settings.SenderName },
            to = new[] { new { email = toEmail } },
            subject,
            htmlContent = htmlBody
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.ApiUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add(settings.ApiKeyHeader, settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (TaskCanceledException ex)
        {
            // Timeout hua (provider slow/down) → ExternalServiceException mein convert
            _logger.LogError(ex, "Email provider timeout for {Email}. Request took longer than {Timeout}s.",
                toEmail, _httpClient.Timeout.TotalSeconds);

            throw new ExternalServiceException(
                "The email service is temporarily slow. Please try again in a moment.");
        }
        catch (HttpRequestException ex)
        {
            // Network issue — DNS fail, connection refused, etc.
            _logger.LogError(ex, "Email provider network error for {Email}.", toEmail);

            throw new ExternalServiceException(
                "We couldn't reach the email service. Please check your internet and try again.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Email provider request failed: {StatusCode} - {ErrorBody}", response.StatusCode, errorBody);
                throw new ExternalServiceException("Unable to send OTP email at the moment. Please try again shortly.");
            }
        }
    }
}