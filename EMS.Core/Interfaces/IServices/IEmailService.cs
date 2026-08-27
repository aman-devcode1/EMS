namespace EMS.Core.Interfaces.ExternalServices;

public interface IEmailService
{
    // oldOtp null hoga first-time send mein, value hoga resend (5 min ke andar) ke case mein
    Task<bool> SendOtpEmailAsync(string toEmail, string newOtpCode, string? oldOtpCode);
}
