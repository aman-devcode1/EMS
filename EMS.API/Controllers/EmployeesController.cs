using AutoMapper;
using EMS.Core.Common;
using EMS.Core.DTOs.Employee;
using EMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // 🔒 JWT Authentication के लिए
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService, IMapper mapper)
    {
        _employeeService = employeeService;
    }

    // ============================================================
    // 1. GET ALL EMPLOYEES (Pagination + Search + Sort + Filter)
    // ============================================================
    [HttpGet]
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
    [HttpGet("{id}")]
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
    // 5. DELETE EMPLOYEE
    // ============================================================
    [HttpDelete("{id}")]
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

    // ============================================================
    // 7. GET EMPLOYEE BY USER ID (Extra Feature — For Auth)
    // ============================================================
    [HttpGet("user/{userId}")]
    [Authorize]  // 🔥 किसी भी Logged-in User को अपना Profile देखने दो
    [ProducesResponseType(typeof(ApiResponse<EmployeeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var result = await _employeeService.GetByUserIdAsync(userId);

        return Ok(new ApiResponse<EmployeeResponseDto>
        {
            Success = true,
            Message = "Employee fetched successfully.",
            Data = result
        });
    }

        // ============================================================
    // 8. PROMOTE EMPLOYEE TO MANAGER (Admin only)
    // ============================================================
    [HttpPatch("{id}/promote-to-manager")]
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
}