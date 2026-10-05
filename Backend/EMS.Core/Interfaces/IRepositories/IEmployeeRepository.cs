using EMS.Core.Common;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;

namespace EMS.Core.Interfaces.IRepositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id);
    Task<Employee?> GetByUserIdAsync(int userId);
    Task<Employee?> GetByEmailAsync(string email);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<PagedResult<Employee>> GetPagedAsync(EmployeeQueryParams queryParams);
}