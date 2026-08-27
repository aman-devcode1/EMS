using System.Security.Claims;
using EMS.Core.Common;
using EMS.Core.DTOs.Auth;
using EMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // ============================================================
    // 1. REGISTER ENDPOINT
    // ============================================================
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        var result = await _authService.RegisterAsync(registerDto);
        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Registration successful. Please check your email for verification (if enabled).",
            Data = result
        });
    }

    // ============================================================
    // 1.1 VERIFY REGISTRATION OTP
    // ============================================================
    [HttpPost("verify-registration-otp")]
    public async Task<IActionResult> VerifyRegistrationOtp([FromBody] VerifyOtpDto verifyOtpDto)
    {
        var result = await _authService.VerifyOtpAsync(verifyOtpDto);
        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Email verification successful. You are now logged in.",
            Data = result
        });
    }

    // ============================================================
    // 2. LOGIN ENDPOINT
    // ============================================================
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // ✅ Service को Call करो (Business Logic वहाँ Handle होगी)
        var result = await _authService.LoginAsync(loginDto);

        // ✅ Standard ApiResponse में Wrap करके Return करो
        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Login successful",
            Data = result
        });
    }


    // ============================================================
    // 3. REFRESH TOKEN ENDPOINT
    // ============================================================
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto refreshTokenRequest)
    {
        var result = await _authService.RefreshTokenAsync(refreshTokenRequest.RefreshToken);

        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Token refreshed successfully",
            Data = result
        });
    }


    // ============================================================
    // 4. Revoke Token (Logout from one device) - 🔒 Requires Auth
    // ============================================================
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeRequestDto revokeTokenRequest)
    {
        // ✅ Service को Call करो (Business Logic वहाँ Handle होगी)
        var result = await _authService.RevokeTokenAsync(revokeTokenRequest.RefreshToken);

        // ✅ (IF-ELSE का सही उपयोग) - Flow Control के लिए
        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Refresh token revocation not found or already revoked.",
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Refresh token revoked successfully.",
        });
    }


    // ============================================================
    // 5. REVOKE ALL TOKENS (Logout from all devices) - 🔒 Requires Auth
    // ============================================================
    [HttpPost("revoke-all-tokens")]
    [Authorize] // 🔒 Requires Auth
    public async Task<IActionResult> RevokeAllTokens()
    {
        // ✅ JWT Token से UserId Claim निकालो
        // (पहले "uid" Claim देखो, न मिले तो NameIdentifier देखो)
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "User ID not found in the token.",
            });
        }

        var userId = int.Parse(userIdClaim.Value);
        await _authService.RevokeAllTokensAsync(userId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "All sessions revoked successfully. You have been logged out from all devices.",
        });
    }
}