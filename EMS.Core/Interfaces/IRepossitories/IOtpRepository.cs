using EMS.Core.Entities;

namespace EMS.Core.Interfaces.IRepositories;

public interface IOtpRepository
{
    // Get existing OTP (without expiry check)
    Task<OtpCode?> GetOtpCodeByUserIdAsync(int userId);

    // 1. Save new OTP (Replace old one if exists)
    Task AddOtpCodeAsync(OtpCode otpCode);
    
    // 3. Delete OTP after use
    Task DeleteOtpCodeAsync(int id);
}