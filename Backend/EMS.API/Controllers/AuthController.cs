using System.Net;
using System.Security.Claims;
using Azure.Core;
using EMS.API.Helper;
using EMS.Core.Common;
using EMS.Core.Dtos.Auth;
using EMS.Core.DTOs.Auth;
using EMS.Core.Interfaces;
using EMS.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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
        return Ok(new ApiResponse<OtpSentResponseDto>
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
        var result = await _authService.VerifyRegistrationOtpAsync(verifyOtpDto);
        CookieHelper.SetRefreshTokenCookie(HttpContext, result.RefreshToken);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Email verification successful. You are now logged in.",
            Data = new
            {
                accessToken = result.AccessToken,
                expiresIn = result.ExpiresIn,
                tokenType = result.TokenType
            }
        });
    }

    // ============================================================
    // 2. LOGIN ENDPOINT
    // ============================================================
    [HttpPost("login")]
    [EnableRateLimiting("auth-strict")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // ✅ Service को Call करो (Business Logic वहाँ Handle होगी)
        var result = await _authService.LoginAsync(loginDto);

        CookieHelper.SetRefreshTokenCookie(HttpContext, result.RefreshToken);

        // ✅ Standard ApiResponse में Wrap करके Return करो
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Login successful",
            Data = new
            {
                accessToken = result.AccessToken,
                expiresIn = result.ExpiresIn,
                tokenType = result.TokenType
            }
        });
    }


    // ============================================================
    // 3. REFRESH TOKEN ENDPOINT
    // - 200: Session refreshed successfully
// - 204: No session (cookie missing OR invalid) — SILENT, not an error
    // ============================================================
[HttpPost("refresh-token")]
[AllowAnonymous]
[EnableRateLimiting("auth-refresh")]
public async Task<IActionResult> RefreshToken()
{
    var refreshToken = CookieHelper.GetRefreshTokenFromCookie(HttpContext);

    if (string.IsNullOrEmpty(refreshToken))
    {
        return NoContent();   // 204 silent
    }

    try
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.RefreshTokenAsync(refreshToken, ip, userAgent);

        CookieHelper.SetRefreshTokenCookie(HttpContext, result.RefreshToken);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Token refreshed successfully",
            Data = new
            {
                accessToken = result.AccessToken,
                expiresIn = result.ExpiresIn,
                tokenType = result.TokenType
            }
        });
    }
    catch (UnauthorizedAccessException)
    {
        CookieHelper.ClearRefreshTokenCookie(HttpContext);
        return NoContent();   // 204 silent
    }
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


    // ============================================================
    // 6. Resend Otp 
    // ============================================================
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto resendOtpDto)
    {
        var result = await _authService.ResendOtpAsync(resendOtpDto);
        return Ok(new ApiResponse<OtpSentResponseDto>
        {
            Success = true,
            Message = "OTP resent successfully.",
            Data = result
        });
    }


    // ============================================================
    // 6. Fsorgot Password - Email -> OTP (Admin/Manager/Emaployee sabhi k liye same endpoint)
    // ============================================================
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
    {
        var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
        return Ok(new ApiResponse<OtpSentResponseDto>
        {
            Success = true,
            Message = "If an account exists for this email address, a password reset code has been sent to it.",
            Data = new OtpSentResponseDto
            {
                Email = result.Email
            }
        });
    }


    // ============================================================
    // 7. Reset Password - Email -> OTP -> New Password
    // ============================================================
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
    {
        var result = await _authService.ResetPasswordAsync(resetPasswordDto);
        return Ok(new ApiResponse<ResetPasswordResponseDto>
        {
            Success = true,
            Message = "Password has been reset successfully.",
            Data = new ResetPasswordResponseDto
            {
                Role = result.Role
            }
        });
    }

    // ============================================================
    // 8. Logout - Clear the cookie + revoke in DB
    // ============================================================
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var refreshToken = CookieHelper.GetRefreshTokenFromCookie(HttpContext);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            try
            {
                await _authService.RefreshTokenAsync(refreshToken);
            }
            catch
            {

            }
        }

        CookieHelper.ClearRefreshTokenCookie(HttpContext);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Logged out successfullly."
        });
    }
}