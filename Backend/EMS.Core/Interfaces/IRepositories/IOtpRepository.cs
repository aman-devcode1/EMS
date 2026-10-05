using EMS.Core.Entities;
using EMS.Core.Enums;

namespace EMS.Core.Interfaces.IRepositories;

public interface IOtpRepository
{
    // Get existing OTP (without expiry check)
    Task<OtpCode?> GetOtpCodeByUserIdAsync(int userId, OtpPurpose? purpose = null);

    // Save new OTP (Replace old one if exists)
    Task AddOtpCodeAsync(OtpCode otpCode);
    
    // Delete OTP after use
    Task DeleteOtpCodeAsync(int id);

    // Delete all expired otps 
    Task DeleteExpiredOtpCodesAsync();

    Task UpdateOtpCodeAsync(OtpCode otpCode);
}