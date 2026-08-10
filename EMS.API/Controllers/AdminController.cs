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
    // 1. ADMIN REGISTER (सिर्फ एक बार - Secret API)
    // ============================================================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAdmin([FromBody] AdminRegisterDto adminRegisterDto)
    {
        var result = await _authService.AdminRegisterAsync(adminRegisterDto);
        return Ok(new ApiResponse<TokenResponseDto>
        {
            Success = true,
            Message = "Admin registration successful. Please check your email for verification (if enabled).",
            Data = result
        });
    }

    // ============================================================
    // 2. ADMIN LOGIN
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

}