using EMS.Core.Common;
using EMS.Core.DTOs.Admin;
using EMS.Core.DTOs.Employee;
using EMS.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IAuthService _authService;

    public EmployeesController(IEmployeeService employeeService, IAuthService authService)
    {
        _employeeService = employeeService;
        _authService = authService;
    }

    // ============================================================
    // 1. GET ALL EMPLOYEES (Pagination + Search + Sort + Filter)
    // ============================================================
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EmployeeResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeQueryParams queryParams)
    {
        var result = await _employeeService.GetEmployeesAsync(queryParams);

        return Ok(new ApiResponse<PagedResult<EmployeeResponseDto>>
        {
            Success = true,
            Message = "Employees fetched successfully.",
            Data = result
        });
    }

    // ============================================================
    // 2. GET EMPLOYEE BY ID
    // ============================================================
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _employeeService.GetByIdAsync(id);

        return Ok(new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Employee fetched successfully.",
            Data = result
        });
    }

    // ============================================================
    // 3. CREATE EMPLOYEE
    // ============================================================
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] EmployeeCreateDto employeeCreateDto)
    {
        var result = await _employeeService.CreateAsync(employeeCreateDto);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Employee created successfully.",
            Data = result
        });
    }

    // ============================================================
    // 4. UPDATE EMPLOYEE
    // ============================================================
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeCreateDto employeeUpdateDto)
    {
        var result = await _employeeService.UpdateAsync(id, employeeUpdateDto);

        return Ok(new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Employee updated successfully.",
            Data = result
        });
    }

    // ============================================================
    // 4.1 UPDATE PROFESSIONAL INFO (Department/Designation/Salary/PrevRole)
    // ============================================================
    [HttpPut("{id:int}/professional")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfessional(int id, [FromBody] UpdateEmployeeProfessionalDto updateEmployeeProfessionalDto)
    {
        var result = await _employeeService.UpdateProfessionalInfoAsync(id, updateEmployeeProfessionalDto);

        return Ok(new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Professional information updated successfully.",
            Data = result
        });
    }

    // ============================================================
    // 5. DELETE EMPLOYEE
    // ============================================================
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _employeeService.DeleteAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Employee deleted successfully.",
            Data = null
        });
    }

    // ============================================================
    // 6. TOGGLE ACTIVE STATUS (Extra Feature)
    // ============================================================
    [HttpPatch("{id}/toggle-active")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _employeeService.ToggleActiveStatusAsync(id);

        return Ok(new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Employee active status toggled successfully.",
            Data = result
        });
    }

    // // ============================================================
    // // 7. GET EMPLOYEE BY USER ID (Extra Feature — For Auth)
    // // ============================================================
    // [HttpGet("user/{userId}")]
    // [Authorize]  // 🔥 किसी भी Logged-in User को अपना Profile देखने दो
    // [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    // [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    // [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    // [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    // public async Task<IActionResult> GetByUserId(int userId)
    // {
    //     var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);

    //     if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var currentUserID))
    //     {
    //         return Unauthorized(new ApiResponse<object>
    //         {
    //             Success = false,
    //             Message = "User ID not found in the Token."
    //         });
    //     }

    //     if (currentUserID != userId)
    //     {
    //         return Forbid();
    //     }

    //     var result = await _employeeService.GetByUserIdAsync(userId);

    //     return Ok(new ApiResponse<EmployeeResponseDto>
    //     {
    //         Success = true,
    //         Message = "Employee fetched successfully.",
    //         Data = result
    //     });
    // }

    // ============================================================
    // 7. GET MY OWN PROFILE
    // ============================================================
    [HttpGet("me")]
    [Authorize]  // 🔥 किसी भी Logged-in User को अपना Profile देखने दो
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        // JWT se user ID nikalo
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);

        // Agar JWT mein userId na mile to
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "User Id not found in the token.",
            });
        }

        // Current login user ka employee profile fetch karo
        var result = await _employeeService.GetByUserIdAsync(userId);

        return Ok(new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Employee data fetched successfully.",
            Data = result
        });
    }

    // ============================================================
    // 8. PROMOTE EMPLOYEE TO MANAGER (Admin only)
    // ============================================================
    [HttpPatch("{id}/promote-to-manager")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PromoteToManager(int id)
    {
        await _employeeService.PromoteToManagerAsync(id);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Employee promoted to Manager successfully.",
            Data = null
        });
    }

    // ============================================================
    // 9. UPDATE MY OWN PROFILE (Employee)
    // ============================================================
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateAdminProfileDto updateAdminProfileDto)
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "User Id not found in token."
            });
        }

        var userId = int.Parse(userIdClaim.Value);

        var emailChanged = await _authService.UpdateAdminProfileAsync(userId, updateAdminProfileDto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = emailChanged
                ? "Profile updated. Email changed - please sign in again."
                : "Profile updated successfully.",
            Data = new { requiresRelogin = emailChanged }
        });
    }


    // ============================================================
    // 10. CHANGE MY PASSWORD (Employee)
    // ============================================================
    [HttpPost("me/change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        var userIdClaim = User.FindFirst("uid") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized(new ApiResponse<object> { Success = false, Message = "User ID not found in token." });

        var userId = int.Parse(userIdClaim.Value);
        await _authService.ChangePasswordAsync(userId, changePasswordDto);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Password changed successfully. Please sign in again.",
            Data = new { requiresRelogin = true }
        });
    }
}