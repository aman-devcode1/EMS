using EMS.Core.Entities;

namespace EMS.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);       // Token से पूरा Record ढूँढो
    Task CreateAsync(RefreshToken refreshToken);             // नया Token Save करो
    Task UpdateAsync(RefreshToken refreshToken);             // Token Update करो (Revoke, Expiry बदलने के लिए)
    Task RevokeAllByUserIdAsync(int userId);                 // किसी User के सारे Tokens Revoke करो (Logout All)
    Task DeleteExpiredTokensAsync();                         // पुराने (Expired) Tokens Cleanup करो (Background Job के लिए)
}   