using EMS.Core.Entities;
using EMS.Core.Enums;
using EMS.Core.Interfaces.ExternalServices;
using EMS.Core.Interfaces.IRepositories;
using EMS.Core.Interfaces.IServices;
using Microsoft.Extensions.Configuration;

namespace EMS.Services.Services;

public class OtpService : IOtpService
{
    private readonly IEmailService _emailService;
    private readonly IOtpRepository _otpRepo;
    private readonly IConfiguration _config;

    public OtpService(IEmailService emailService, IOtpRepository otpRepo, IConfiguration config)
    {
        _emailService = emailService;
        _otpRepo = otpRepo;
        _config = config;
    }

    // ============================================================
    // 1. GENERATE AND SEND OTP (UserId + Purpose)
    // ============================================================
    public async Task<bool> GenerateAndSendOtpAsync(int userId, string email, OtpPurpose purpose)
    {
        // Fetch existing OTP
        var existingOtp = await _otpRepo.GetOtpCodeByUserIdAsync(userId);
        string? oldCodeToShow = null;

        if (existingOtp != null)
        {
            if (existingOtp.ExpiresAt > DateTime.UtcNow)
                oldCodeToShow = existingOtp.Code;
            await _otpRepo.DeleteOtpCodeAsync(existingOtp.Id);
        }

        // Generate new OTP
        var newCode = GenerateOtpCode();
        var otpEntity = new OtpCode
        {
            UserId = userId,
            Code = newCode,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
            // CreatedAt auto-set from BaseEntity
        };

        await _otpRepo.AddOtpCodeAsync(otpEntity);

        // Send email
        var emailSent = await _emailService.SendOtpEmailAsync(email, newCode, oldCodeToShow);

        // Rollback if email fails
        if (!emailSent)
        {
            await _otpRepo.DeleteOtpCodeAsync(otpEntity.Id);
            return false;
        }

        return true;
    }

    // ============================================================
    // 2. VERIFY OTP (UserId + Code)
    // ============================================================
    public async Task<bool> VerifyOtpAsync(int userId, string otpCode)
    {
        var otp = await _otpRepo.GetOtpCodeByUserIdAsync(userId);

        if (otp == null || otp.ExpiresAt < DateTime.UtcNow || otp.Code != otpCode)
            return false;

        await _otpRepo.DeleteOtpCodeAsync(otp.Id);
        return true;
    }

    // ============================================================
    // 3. RESEND OTP (delete old → generate new)
    // ============================================================
    public async Task<bool> ResendOtpAsync(int userId, string email, OtpPurpose purpose)
    {
        return await GenerateAndSendOtpAsync(userId, email, purpose);
    }

    // ============================================================
    // PRIVATE HELPER
    // ============================================================
    private string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}