using EMS.Core.Entities;
using EMS.Core.Interfaces.IRepositories;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public class OtpRepository : IOtpRepository
{
    private readonly AppDbContext _context;

    public OtpRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OtpCode?> GetOtpCodeByUserIdAsync(int userId)
    {
        return await _context.OtpCodes.FirstOrDefaultAsync(o => o.UserId == userId);
    }

    public async Task AddOtpCodeAsync(OtpCode otpCode)
    {
        // Add new OTP
        await _context.OtpCodes.AddAsync(otpCode);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOtpCodeAsync(int id)
    {
        var otp = await _context.OtpCodes.FindAsync(id);
        if (otp != null)
        {
            _context.OtpCodes.Remove(otp);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteExpiredOtpCodesAsync()
    {
        var expiredOtps = await _context.OtpCodes
            .Where(o => o.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();

        if (expiredOtps.Count > 0)
        {
            _context.OtpCodes.RemoveRange(expiredOtps);
            await _context.SaveChangesAsync();
        }
    }
}