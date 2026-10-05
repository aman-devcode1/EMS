using System.ComponentModel.Design;
using AutoMapper;
using EMS.Core.Common;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;
using EMS.Core.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces;
using EMS.Core.Interfaces.IRepositories;
using EMS.Core.Interfaces.IServices;

namespace EMS.Services.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDesignationRepository _designationRepository;
    private readonly IMapper _mapper;

    public EmployeeService(IEmployeeRepository employeeRepository, IUserRepository userRepository, IDepartmentRepository departmentRepository, IDesignationRepository designationRepository, IMapper mapper)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _designationRepository = designationRepository;
        _mapper = mapper;
    }

    // ============================================================
    // 1. GET EMPLOYEE BY ID
    // ============================================================
    public async Task<EmployeeResponseDto?> GetByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);

        if (employee == null)
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
    // 3b. UPDATE PROFESSIONAL INFO (Admin only)
    // Only: Salary, Department, Designation, Previous Company Role
    // Personal + System fields stay untouched.
    // ============================================================
    public async Task<EmployeeResponseDto> UpdateProfessionalInfoAsync(int id, UpdateEmployeeProfessionalDto updateEmployeeProfessionalDto)
    {
        var employee = await _employeeRepository.GetByIdAsync(id)
            ?? throw new NotFoundException($"Employee with Id {id} not found.");

        // Salary validation (business rule)
        if (updateEmployeeProfessionalDto.Salary.HasValue && updateEmployeeProfessionalDto.Salary.Value < 10000)
            throw new BadRequestException("Salary must be at least Rs.10,000.");

        // Validate Department (if provided)
        if (updateEmployeeProfessionalDto.DepartmentId.HasValue)
        {
            var dept = await _departmentRepository.GetByIdAsync(updateEmployeeProfessionalDto.DepartmentId.Value);
            if (dept == null || !dept.IsActive)
                throw new BadRequestException("Selected department is invalid or inactive.");
        }

        // Validate Designation (if provided)
        if (updateEmployeeProfessionalDto.DesignationId.HasValue)
        {
            var desig = await _designationRepository.GetByIdAsync(updateEmployeeProfessionalDto.DesignationId.Value);
            if (desig == null || !desig.IsActive)
                throw new BadRequestException("Selected designation is invalid or inactive.");
        }

        // Update ONLY professional fields
        if (updateEmployeeProfessionalDto.Salary.HasValue) employee.Salary = updateEmployeeProfessionalDto.Salary.Value;
        employee.DepartmentId = updateEmployeeProfessionalDto.DepartmentId;
        employee.DesignationId = updateEmployeeProfessionalDto.DesignationId;

        if (!string.IsNullOrWhiteSpace(updateEmployeeProfessionalDto.PreviousCompanyRole))
            employee.PreviousCompanyRole = updateEmployeeProfessionalDto.PreviousCompanyRole.Trim();

        employee.UpdatedAt = DateTime.UtcNow;

        await _employeeRepository.UpdateAsync(employee);

        // Reload with includes for accurate DTO mapping
        var updated = await _employeeRepository.GetByIdAsync(id);
        return _mapper.Map<EmployeeResponseDto>(updated);
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
    public async Task<EmployeeResponseDto> GetByUserIdAsync(int userId)
    {
        var employee = await _employeeRepository.GetByUserIdAsync(userId);

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

    // ============================================================
    // 8. PROMOTE TO MANAGER (Admin action)
    // ============================================================
    public async Task<bool> PromoteToManagerAsync(int employeeId)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId);

        if (employee == null)
            throw new NotFoundException($"Employee with Id {employeeId} not found.");

        if (employee.User == null)
            throw new BadRequestException("This employee does not have a user account.");

        if (employee.User.Role == RoleType.Manager)
            throw new BadRequestException("This employee is already a Manager.");

        employee.User.Role = RoleType.Manager;
        await _userRepository.UpdateAsync(employee.User);

        return true;
    }
}