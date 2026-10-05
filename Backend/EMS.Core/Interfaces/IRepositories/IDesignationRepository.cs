using EMS.Core.Entities;

namespace EMS.Core.Interfaces.IRepositories;

public interface IDesignationRepository
{
    Task<List<Designation>> GetAllAsync(bool includeInactive = false);
    Task<Designation?> GetByIdAsync(int Id);
    Task<Designation?> GetByNameAsync(string name);
    Task AddAsync(Designation designation);
    Task UpdateAsync(Designation designation);
}