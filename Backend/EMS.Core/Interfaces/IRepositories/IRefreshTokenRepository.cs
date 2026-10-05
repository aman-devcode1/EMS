using EMS.Core.Entities;

namespace EMS.Core.Interfaces.IRepositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task<RefreshToken?> GetActiveByUserIdAsync(int userId);
    Task CreateAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAsync(RefreshToken refreshToken, string? reason = null, string? ip = null);             
    Task RevokeAllByUserIdAsync(int userId, string? reason = null, string? ip = null);
    Task DeleteExpiredTokensAsync();                         
}   