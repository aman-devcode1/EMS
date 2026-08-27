using EMS.Core.Entities;
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

    public async Task<bool> GenerateAndSendOtpAsync(string email)
    {
         // 1. Generate 6-digit OTP
        var otpCode = new Random().Next(100000, 999999).ToString();
        var expiry = DateTime.UtcNow.AddMinutes(5);

        // 2. Create OTP Record
        var otpRecord = new OtpRecord
        {
            Email = email,
            OtpCode = otpCode,
            OtpExpiryTime = expiry,
            OtpIsUsed = false
        };

        // 3. Save/Replace in DB (Upsert)
        await _otpRepo.SaveOrUpdateOtpAsync(otpRecord);

        // 4. Send Email
        var emailSent = await _emailService.SendOtpEmailAsync(email, otpCode);

        // 5. If email failed, delete the OTP from DB to avoid stale data
        if (!emailSent)
        {
            await _otpRepo.DeleteOtpAsync(email);
            return false;
        }

        return true;
    }

    public async Task<bool> VerifyAndDeleteOtpAsync(string email, string otpCode)
    {
        // 1. Get Valid OTP
        var otpRecord = await _otpRepo.GetValidOtpAsync(email, otpCode);

        if (otpRecord == null)
            return false; // Invalid or expired

        // 2. Delete OTP immediately (Secure)
        await _otpRepo.DeleteOtpAsync(email);

        return true;
    }

    private string GenerateOtpCode()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString(); // Generates a 6-digit OTP
    }
}