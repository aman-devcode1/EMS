using EMS.Core.DTOs;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;

namespace EMS.Core.Interfaces;

public interface IAuthService
{
    // 🔥 1. LOGIN - Ab TokenResponseDto Return karega (Access + Refresh)
    Task<TokenResponseDto> LoginAsync(LoginDto loginDto);

    // 🔥 2. REGISTER - Naya User + Employee Create karega, aur Tokens Return karega
    // (Note: Humne RegisterDto mein Personal Details rakhi hain)
    Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto);

    // 🔥 3. REFRESH TOKEN - Purane RefreshToken se naya AccessToken लेना
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);

    // 🔥 4. REVOKE TOKEN - Logout (एक Device को Logout करना)
    Task<bool> RevokeTokenAsync(string refreshToken);

    // 🔥 5. REVOKE ALL TOKENS - Logout All Devices (Admin Feature)
    Task<bool> RevokeAllTokensAsync(int userId);

    // 🔥 6. Admin Methods (Naya Add)
    Task<TokenResponseDto> AdminRegisterAsync(AdminRegisterDto adminRegisterDto);
    Task<TokenResponseDto> AdminLoginAsync(AdminLoginDto adminLoginDto);

}
