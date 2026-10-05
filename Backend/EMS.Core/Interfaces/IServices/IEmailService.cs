using EMS.Core.Enums;

namespace EMS.Core.Interfaces.ExternalServices;

public interface IEmailService
{
    // oldOtp null hoga first-time send mein, value hoga resend (5 min ke andar) ke case mein
    Task<bool> SendOtpEmailAsync(string toEmail, string newOtpCode, bool isResend, OtpPurpose purpose = OtpPurpose.Registration);
    // Task<bool> SendOtpEmailAsync(string toEmail, string newOtpCode, string? oldOtpCode = null);
    // object SendOtpEmailInBackgroungAsync(string email, string newCode, string? oldCodeToShow);

    // Password successfully reset hone ke baad best-effort confirmation —
    // agar user ne khud reset nahi kiya tha to usse turant pata chal jaaye.
    Task<bool> SendPasswordChangedEmailAsync(string toEmail);
}
