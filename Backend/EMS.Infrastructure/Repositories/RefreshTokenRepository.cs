using EMS.Core.Entities;
using EMS.Core.Interfaces.IRepositories;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _context;
    public RefreshTokenRepository(AppDbContext context) => _context = context;

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash) =>
        await _context.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u!.Employee)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

    public async Task<RefreshToken?> GetActiveByUserIdAsync(int userId) =>
        await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.AbsoluteExpiresAt > DateTime.UtcNow)
            .OrderByDescending(rt => rt.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task CreateAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken token)
    {
        _context.RefreshTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAsync(RefreshToken refreshToken, string? reason = null, string? ip = null)
    {
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedReason = reason;
        refreshToken.RevokedByIp = ip;
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllByUserIdAsync(int userId, string? reason = null, string? ip = null)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = DateTime.UtcNow;
            t.RevokedReason = reason ?? "Revoked";
            t.RevokedByIp = ip;
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteExpiredTokensAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7); // Retain for audit for 7 days
        var expired = await _context.RefreshTokens
            .Where(rt => rt.AbsoluteExpiresAt < cutoff)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(expired);
            await _context.SaveChangesAsync();
        }
    }
}