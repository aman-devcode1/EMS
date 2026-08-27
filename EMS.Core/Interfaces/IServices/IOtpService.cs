namespace EMS.Core.Interfaces.IServices;

public interface IOtpService
{
    // Generate OTP, Save to DB, Send Email
    Task<bool> GenerateAndSendOtpAsync(string email);

    // Verify OTP, Delete on success
    Task<bool> VerifyAndDeleteOtpAsync(string email, string otpCode);
}