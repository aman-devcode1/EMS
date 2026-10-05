using EMS.Core.Entities;

namespace EMS.Core.Interfaces.IRepositories;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync(bool includeInactive = false);
    Task<Department?> GetByIdAsync(int Id);
    Task<Department?> GetByNameAsync(string name);
    Task AddAsync(Department department);
    Task UpdateAsync(Department department);
}