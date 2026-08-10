using EMS.Core.Common;
using EMS.Core.DTOs;
using EMS.Core.DTOs.Employee;

namespace EMS.Core.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeResponseDto?> GetByIdAsync(int id);
    Task<PagedResult<EmployeeResponseDto>> GetEmployeesAsync(EmployeeQueryParams queryParams);
    Task<EmployeeResponseDto> CreateAsync(EmployeeCreateDto dto);
    Task<EmployeeResponseDto?> UpdateAsync(int id, EmployeeCreateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<EmployeeResponseDto> ToggleActiveStatusAsync(int id);
    Task<EmployeeResponseDto> GetByUserIdAsync(int id);
}