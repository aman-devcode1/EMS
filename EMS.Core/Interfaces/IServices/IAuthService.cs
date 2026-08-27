using EMS.Core.DTOs;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;
using EMS.Core.Dtos.Auth;

namespace EMS.Core.Interfaces.IServices;

public interface IAuthService
{
    // 🔥 1. LOGIN - Ab TokenResponseDto Return karega (Access + Refresh)
    Task<TokenResponseDto> LoginAsync(LoginDto loginDto);

    // 🔥 EMPLOYEE: Register karega, OTP jayega (tokens turant nahi)
    Task<OtpSentResponseDto> RegisterAsync(RegisterDto registerDto);

    // 🔥 3. REFRESH TOKEN - Purane RefreshToken se naya AccessToken लेना
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);

    // 🔥 4. REVOKE TOKEN - Logout (एक Device को Logout करना)
    Task<bool> RevokeTokenAsync(string refreshToken);

    // 🔥 5. REVOKE ALL TOKENS - Logout All Devices (Admin Feature)
    Task<bool> RevokeAllTokensAsync(int userId);

    // 🔥 ADMIN: Register karega, OTP jayega
    Task<OtpSentResponseDto> AdminRegisterAsync(AdminRegisterDto adminRegisterDto);

    // 🔥 ADMIN + MANAGER: Password/Phone check ke baad OTP bhejega (tokens nahi)
    Task<OtpSentResponseDto> AdminLoginAsync(AdminLoginDto adminLoginDto);

    // 🔥 Registration OTP verify (Employee + Admin dono) — verify hone par tokens milenge
    Task<TokenResponseDto> VerifyRegistrationOtpAsync(VerifyOtpDto verifyOtpDto);

    // 🔥 Admin/Manager Login OTP verify — verify hone par tokens milenge
    Task<TokenResponseDto> VerifyAdminLoginOtpAsync(VerifyOtpDto verifyOtpDto);

}
