

using System.Data.Common;
using AutoMapper;
using EMS.Core.Common;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces;
using EMS.Infrastructure.Data;

namespace EMS.Services.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public EmployeeService(IEmployeeRepository employeeRepository, IUserRepository userRepository, IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    // ============================================================
    // 1. GET EMPLOYEE BY ID
    // ============================================================
    public async Task<EmployeeResponseDto?> GetByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee != null)
            throw new NotFoundException($"Employee with Id{id} not found.");

        return _mapper.Map<EmployeeResponseDto>(employee);
    }

    // ============================================================
    // 2. CREATE EMPLOYEE (Admin से)
    // ============================================================
    public async Task<EmployeeResponseDto> CreateAsync(EmployeeCreateDto employeeCreateDto)
    {
        // 🔥 Business Rule 1: Minimum Salary Check (₹10,000)
        if (employeeCreateDto.Salary < 10000)
            throw new BadRequestException("Salary must be atleast Rs.10000");

        // 🔥 Business Rule 2: Duplicate Email Check
        var existingUser = await _userRepository.GetByEmailAsync(employeeCreateDto.Email);

        if (existingUser != null)
            throw new ConflictException($"Email '{employeeCreateDto.Email}' is already registered.");

        // 🔥 Business Rule 3: Department Validation (अगर Department Empty है तो Default Set करो)
        if (string.IsNullOrWhiteSpace(employeeCreateDto.Department))
            employeeCreateDto.Department = "General";

        // 🔥 DTO → Entity में Map करो (AutoMapper)
        var employee = _mapper.Map<Employee>(employeeCreateDto);

        // 🔥 System Fields Set करो
        employee.IsActive = true;
        employee.CreatedAt = DateTime.UtcNow;

        // 🔥 Repository में Save करो
        await _employeeRepository.AddAsync(employee);

        // ✅ Fix 4: Entity → Response DTO में Map करो और Return करो!
        return _mapper.Map<EmployeeResponseDto>(employee);
    }

    // ============================================================
    // 3. UPDATE
    // ============================================================
    public async Task<EmployeeResponseDto?> UpdateAsync(int id, EmployeeCreateDto employeeUpdateDto)
    {
        var existingEmployee = await _employeeRepository.GetByIdAsync(id);

        if (existingEmployee == null)
            throw new NotFoundException($"Employee with Id{id} not found.");

        if (employeeUpdateDto.Salary < 10000)
            throw new BadRequestException("Salary must be atleast Rs.10000");

        // Emil Change Check
        if (!string.Equals(existingEmployee.Email, employeeUpdateDto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existUser = await _userRepository.GetByEmailAsync(employeeUpdateDto.Email);

            if (existUser != null)
                throw new ConflictException($"Email '{employeeUpdateDto.Email}' is already registered.");
        }

        // Map DTO → Existing Entity
        _mapper.Map(employeeUpdateDto, existingEmployee);
        existingEmployee.UpdatedAt = DateTime.UtcNow;

        await _employeeRepository.UpdateAsync(existingEmployee);

        return _mapper.Map<EmployeeResponseDto>(existingEmployee);
    }

    // ============================================================
    // 4. DELETE
    // ============================================================
    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee == null)
            throw new NotFoundException($"Employee with Id {id} not found.");

        await _employeeRepository.DeleteAsync(id);
        return true;
    }

    // ============================================================
    // 5. GET ALL EMPLOYEES (Paged)
    // ============================================================
    public async Task<PagedResult<EmployeeResponseDto>> GetEmployeesAsync(EmployeeQueryParams queryParams)
    {
        var pageResult = await _employeeRepository.GetPagedAsync(queryParams);
        var employeesDto = _mapper.Map<List<EmployeeResponseDto>>(pageResult.Items);

        return new PagedResult<EmployeeResponseDto>
        {
          PageNumber = pageResult.PageNumber,
          PageSize = pageResult.PageSize,
          TotalRecords = pageResult.TotalRecords,  
          TotalPages = pageResult.TotalPages,
          Items = employeesDto  
        };
    }

    // ============================================================
    // 6. GET BY USER ID (Optional)
    // ============================================================
    public async Task<EmployeeResponseDto?> GetByUserIdAsync(int userId)
    {
        var employee = await _employeeRepository.GetByIdAsync(userId);

        if (employee == null)
            throw new NotFoundException($"Employee with UserId {userId} not found.");

        return _mapper.Map<EmployeeResponseDto>(employee);
    }

    // ============================================================
    // 7. TOGGLE ACTIVE STATUS (Optional)
    // ============================================================
    public async Task<EmployeeResponseDto> ToggleActiveStatusAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee == null)
            throw new NotFoundException($"Employee with Id {id} not found.");

        employee.IsActive = !employee.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;

        await _employeeRepository.UpdateAsync(employee);

        return _mapper.Map<EmployeeResponseDto>(employee);
    }
}