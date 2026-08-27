using EMS.Core.Common;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;
using EMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;

    public AdminController(IAuthService authService)
    {
        _authService = authService;
    }

    // ============================================================
    // 1. ADMIN REGISTER (सिर्फ एक बार - Secret API) - OTP Send
    // ============================================================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAdmin([FromBody] AdminRegisterDto adminRegisterDto)
    {
        var result = await _authService.AdminRegisterAsync(adminRegisterDto);
        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Admin registration successful. Please verify the OTP sent to your email address.",
            Data = result
        });
    }

    // ============================================================
    // 2. ADMIN / Manager LOGIN Step 1: Send OTP not Token
    // ============================================================
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAdmin([FromBody] AdminLoginDto adminLoginDto)
    {
        var result = await _authService.AdminLoginAsync(adminLoginDto);
        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Admin login successful",
            Data = result
        });
    }

    // ============================================================
    // 3. VERIFY LOGIN OTP — Step 2 (Tokens milenge)
    // ============================================================
    [HttpPost("verify-login-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyOtpDto verifyOtpDto)
    {
        var result = await _authService.VerifyAdminLoginOtpAsync(verifyOtpDto);
        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Login successful.",
            Data = result
        });
    }

}