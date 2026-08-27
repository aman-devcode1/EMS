using EMS.Core.Common;
using EMS.Core.DTOs;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;

namespace EMS.Core.Interfaces.IRepositories;

public interface IEmployeeRepository
{
    Task<EmployeeResponseDto?> GetByIdAsync(int id);
    Task<PagedResult<EmployeeResponseDto>> GetEmployeesAsync(EmployeeQueryParams queryParams);
    Task<EmployeeResponseDto> CreateAsync(Employee employee);
    Task<EmployeeResponseDto?> UpdateAsync(int id, EmployeeCreateDto dto);
    Task<bool> DeleteAsync(int id);
    Task<EmployeeResponseDto> ToggleActiveStatusAsync(int id);
    Task<EmployeeResponseDto> GetByUserIdAsync(int id);
    Task<bool> PromoteToManagerAsync(int employeeId);
}