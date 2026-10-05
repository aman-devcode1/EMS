using EMS.Core.Common;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;
using EMS.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Core.Dtos.Auth;
using System.Security.Claims;
using EMS.Core.DTOs.MasterData;
using EMS.Core.DTOs.Employee;
using EMS.API.Helper;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDepartmentService _departmentService;
    private readonly IDesignationService _designationService;
    private readonly IEmployeeService _employeeService;

    public AdminController(IAuthService authService, IDepartmentService departmentService, IDesignationService designationService, IEmployeeService employeeService)
    {
        _authService = authService;
        _departmentService = departmentService;
        _designationService = designationService;
        _employeeService = employeeService;
    }

    // ============================================================
    // 1. ADMIN REGISTER (सिर्फ एक बार - Secret API) - OTP Send
    // ============================================================
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterAdmin([FromBody] AdminRegisterDto adminRegisterDto)
    {
        var result = await _authService.AdminRegisterAsync(adminRegisterDto);
        return Ok(new ApiResponse<OtpSentResponseDto>
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
        return Ok(new ApiResponse<OtpSentResponseDto>
        {
            Success = true,
            Message = "OTP sent to your registered email address. Please verify to complete login.",
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
        CookieHelper.SetRefreshTokenCookie(HttpContext, result.RefreshToken);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Login successful.",
            Data = new
            {
                accessToken = result.AccessToken,
                expiresIn = result.ExpiresIn,
                tokenType = result.TokenType
            }
        });
    }

    // ============================================================
    // 4. VERIFY ADMIN REGISTRATION OTP — Step 2 (Tokens milenge)
    // ============================================================
    [HttpPost("verify-registration-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyRegistrationOtp([FromBody] VerifyOtpDto verifyOtpDto)
    {
        var result = await _authService.VerifyRegistrationOtpAsync(verifyOtpDto);
        CookieHelper.SetRefreshTokenCookie(HttpContext, result.RefreshToken);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Admin registration verified successfully. You are now logged in.",
            Data = new
            {
                accessToken = result.AccessToken,
                expiresIn = result.ExpiresIn,
                tokenType = result.TokenType
            }
        });
    }

    // ============================================================
    // 5. Resend Otp 
    // ============================================================
    [HttpPost("resend-otp")]
    [AllowAnonymous]
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
    // 6. REPLACE ADMIN (Sirf current Admin hi kar sakta hai)
    // ============================================================
    [HttpPost("replace-admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReplaceAdmin([FromBody] ReplaceAdminDto replaceAdminDto)
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID not found in token." });

        var currentAdminId = int.Parse(userIdClaim.Value);
        var result = await _authService.ReplaceAdminAsync(currentAdminId, replaceAdminDto);

        return Ok(new ApiResponse<OtpSentResponseDto>
        {
            Success = true,
            Message = "New admin registered. An OTP has been sent to their email for verification. You have been logged out from all devices.",
            Data = result
        });
    }

    // ============================================================
    // 7. GET MY PROFILE (Admin)
    // ============================================================
    [HttpGet("me")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID not found in token." });
        var userId = int.Parse(userIdClaim.Value);

        var profile = await _authService.GetAdminProfileAsync(userId);

        return Ok(new ApiResponse<AdminProfileDto>
        {
            Success = true,
            Message = "",
            Data = profile
        });
    }

    // ============================================================
    // 8. UPDATE MY PROFILE (Admin)
    // ============================================================
    [HttpPut("me")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateAdminProfileDto dto)
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID not found in token." });
        var userId = int.Parse(userIdClaim.Value);

        var emailChanged = await _authService.UpdateAdminProfileAsync(userId, dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = emailChanged
                ? "Profile updated. Email changed — please sign in again."
                : "Profile updated successfully.",
            Data = new { requiresRelogin = emailChanged }
        });
    }

    // ============================================================
    // 9. CHANGE PASSWORD (Admin)
    // ============================================================
    [HttpPost("change-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID not found in token." });
        var userId = int.Parse(userIdClaim.Value);

        await _authService.ChangePasswordAsync(userId, dto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Password changed successfully. Please sign in again.",
            Data = new { requiresRelogin = true }
        });
    }

    // ============================================================
    // 10. GET CURRENT MANAGER 
    // ============================================================
    [HttpGet("manager")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetManager()
    {
        var managerId = await _authService.GetManagerUserIdAsync();
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "",
            Data = new { managerId }
        });
    }

    // ============================================================
    // 11. SET MANAGER ROLE - set the role to manager (toggel)
    // ============================================================
    [HttpPatch("manager/{userId:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetManagerRole(int userId, [FromBody] SetManagerRoleDto setManagetRoleDto)
    {
        await _authService.SetManagerRoleAsync(userId, setManagetRoleDto.IsManager);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = setManagetRoleDto.IsManager
                ? "Employee promoted to Manager. They'll need to sign in again."
                : "Manager demoted to Employee. They'll need to sign in again."
        });
    }

    // ============================================================
    // 12. DEPARTMENTS — GET ALL
    // ============================================================
    [HttpGet("departments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDepartments([FromQuery] bool includeInactive = false)
    {
        var result = await _departmentService.GetAllAsync(includeInactive);
        return Ok(new ApiResponse<List<DepartmentDto>>
        {
            Success = true,
            Message = "",
            Data = result
        });
    }

    // ============================================================
    // 13. DEPARTMENTS — CREATE
    // ============================================================
    [HttpPost("departments")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDepartment([FromBody] CreateUpdateDepartmentDto dto)
    {
        var result = await _departmentService.CreateAsync(dto);
        return Ok(new ApiResponse<DepartmentDto>
        {
            Success = true,
            Message = "Department created successfully.",
            Data = result
        });
    }

    // ============================================================
    // 14. DEPARTMENTS — UPDATE
    // ============================================================
    [HttpPut("departments/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDepartment(int id, [FromBody] CreateUpdateDepartmentDto dto)
    {
        var result = await _departmentService.UpdateAsync(id, dto);
        return Ok(new ApiResponse<DepartmentDto>
        {
            Success = true,
            Message = "Department updated successfully.",
            Data = result
        });
    }

    // ============================================================
    // 15. DEPARTMENTS — TOGGLE ACTIVE
    // ============================================================
    [HttpPatch("departments/{id:int}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleDepartment(int id)
    {
        var result = await _departmentService.ToggleActiveAsync(id);
        return Ok(new ApiResponse<DepartmentDto>
        {
            Success = true,
            Message = result.IsActive ? "Department activated." : "Department deactivated.",
            Data = result
        });
    }

    // ============================================================
    // 16. DESIGNATIONS — GET ALL
    // ============================================================
    [HttpGet("designations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDesignations([FromQuery] bool includeInactive = false)
    {
        var result = await _designationService.GetAllAsync(includeInactive);
        return Ok(new ApiResponse<List<DesignationDto>>
        {
            Success = true,
            Message = "",
            Data = result
        });
    }

    // ============================================================
    // 17. DESIGNATIONS — CREATE
    // ============================================================
    [HttpPost("designations")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDesignation([FromBody] CreateUpdateDesignationDto dto)
    {
        var result = await _designationService.CreateAsync(dto);
        return Ok(new ApiResponse<DesignationDto>
        {
            Success = true,
            Message = "Designation created successfully.",
            Data = result
        });
    }

    // ============================================================
    // 18. DESIGNATIONS — UPDATE
    // ============================================================
    [HttpPut("designations/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDesignation(int id, [FromBody] CreateUpdateDesignationDto dto)
    {
        var result = await _designationService.UpdateAsync(id, dto);
        return Ok(new ApiResponse<DesignationDto>
        {
            Success = true,
            Message = "Designation updated successfully.",
            Data = result
        });
    }

    // ============================================================
    // 19. DESIGNATIONS — TOGGLE ACTIVE
    // ============================================================
    [HttpPatch("designations/{id:int}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleDesignation(int id)
    {
        var result = await _designationService.ToggleActiveAsync(id);
        return Ok(new ApiResponse<DesignationDto>
        {
            Success = true,
            Message = result.IsActive ? "Designation activated." : "Designation deactivated.",
            Data = result
        });
    }

    // ============================================================
    // 20. UPDATE EMPLOYEE PROFESSIONAL INFO
    // ============================================================
    [HttpPut("employees/{id:int}/professional")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateEmployeeProfessional(
        int id,
        [FromBody] UpdateEmployeeProfessionalDto updateEmployeeProfessionalDto)
    {
        var result = await _employeeService.UpdateProfessionalInfoAsync(id, updateEmployeeProfessionalDto);

        return Ok(new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Employee professional information updated successfully.",
            Data = result
        });
    }
}