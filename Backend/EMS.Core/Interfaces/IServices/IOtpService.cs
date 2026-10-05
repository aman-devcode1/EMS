using EMS.Core.Enums;

namespace EMS.Core.Interfaces.IServices;

public interface IOtpService
{
    // Generate OTP with UserId and Purpose
    Task<bool> GenerateAndSendOtpAsync(int userId, string email, OtpPurpose purpose);

    // Verify OTP by UserId and Code
    Task<bool> VerifyOtpAsync(int userId, string otpCode, OtpPurpose purpose);

    // Resend OTP
    Task<bool> ResendOtpAsync(int userId, string email, OtpPurpose purpose);
}