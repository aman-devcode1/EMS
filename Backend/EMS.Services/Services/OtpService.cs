using System.Security.Cryptography;
using EMS.Core.Configuration;
using EMS.Core.Entities;
using EMS.Core.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.ExternalServices;
using EMS.Core.Interfaces.IRepositories;
using EMS.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EMS.Services.Services;

public class OtpService : IOtpService
{
    private readonly IBackgroundEmailQueue _backgroundEmailQueue;
    private readonly IOtpRepository _otpRepo;
    private readonly IEmailStatusStore _emailStatusStore;
    private readonly ILogger<OtpService> _logger;
    private readonly OtpSettings _otp;   // 🔧 NEW

    public OtpService(IBackgroundEmailQueue backgroundEmailQueue, IOtpRepository otpRepo, IEmailStatusStore emailStatusStore, ILogger<OtpService> logger, IOptions<OtpSettings> otpSettings)
    {
        _backgroundEmailQueue = backgroundEmailQueue;
        _otpRepo = otpRepo;
        _emailStatusStore = emailStatusStore;
        _logger = logger;
        _otp = otpSettings.Value;   // 🔧 NEW (IConfiguration hata diya)
    }

    // ============================================================
    // 1. GENERATE AND SEND OTP (UserId + Purpose)
    // ============================================================
    public async Task<bool> GenerateAndSendOtpAsync(int userId, string email, OtpPurpose purpose)
    {
        await _otpRepo.DeleteExpiredOtpCodesAsync();

        var existingOtp = await _otpRepo.GetOtpCodeByUserIdAsync(userId);
        if (existingOtp != null && existingOtp.Purpose == purpose)
        {
            var age = (DateTime.UtcNow - existingOtp.CreatedAt).TotalSeconds;
            var lastMailFailed = _emailStatusStore.Get(email).Status == EmailDeliveryStatus.Failed;

            if (age < _otp.ResendCooldownSeconds && !lastMailFailed)   // 🔧 config se
            {
                if (purpose == OtpPurpose.PasswordReset) return true;
                throw new BadRequestException($"Please wait {Math.Ceiling(_otp.ResendCooldownSeconds - age)} seconds before requesting a new OTP.");
            }
        }

        var isResend = existingOtp != null && existingOtp.ExpiresAt > DateTime.UtcNow;
        if (existingOtp != null)
        {
            await _otpRepo.DeleteOtpCodeAsync(existingOtp.Id);
        }

        var newCode = GenerateOtpCode();
        await _otpRepo.AddOtpCodeAsync(new OtpCode
        {
            UserId = userId,
            Code = newCode,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_otp.ExpiryMinutes),   // 🔧 config se
            IsActive = true
        });

        // 🔧 FIX: pehle "==" tha, yahan "!=" chahiye (PasswordReset ka status track nahi karte)
        if (purpose != OtpPurpose.PasswordReset)
            _emailStatusStore.MarkQueued(email);

        // 🔥 Ab Brevo ko turant call nahi karte — queue me daal do, background worker bhejega.
        // Isse ye method turant return karta hai (sirf DB-bound), chahe Brevo slow ho ya down ho.
        _backgroundEmailQueue.QueueOtpEmail(email, newCode, isResend, purpose);

        if (_otp.LogOtpToConsole)   // 🔧 config se
            _logger.LogWarning("DEV OTP for {Email} ({Purpose}): {OtpCode}", email, purpose, newCode);

        return true;
    }

    // ============================================================
    // 2. VERIFY OTP (UserId + Code)
    // ============================================================
    public async Task<bool> VerifyOtpAsync(int userId, string otpCode, OtpPurpose purpose)
    {
        var otp = await _otpRepo.GetOtpCodeByUserIdAsync(userId);

        if (otp == null) return false;

        // Security: Purpose match जरूरी (Registration का OTP Login में न चले)
        if (otp.Purpose != purpose) return false;

        if (otp.IsUsed) return false;

        if (otp.ExpiresAt <= DateTime.UtcNow || !otp.IsActive) return false;

        // Brute-force roke
        if (otp.FailedAttempts >= _otp.MaxFailedAttempts)   // 🔧 config se
        {
            await _otpRepo.DeleteOtpCodeAsync(otp.Id);
            return false;
        }

        if (!string.Equals(otp.Code, otpCode.Trim(), StringComparison.Ordinal))
        {
            otp.FailedAttempts++;
            await _otpRepo.UpdateOtpCodeAsync(otp);
            return false;
        }

        // right otp - IsUsed = true kare, fir delete
        otp.IsUsed = true;
        await _otpRepo.UpdateOtpCodeAsync(otp);
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
        var length = Math.Clamp(_otp.CodeLength, 4, 9);   // 🔧 config se
        var min = (int)Math.Pow(10, length - 1);
        var max = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(min, max).ToString();
    }
}