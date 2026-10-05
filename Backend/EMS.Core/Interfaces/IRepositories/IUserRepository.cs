using EMS.Core.Entities;

namespace EMS.Core.Interfaces.IRepositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
    Task<bool> ExistsByEmailAsync(string email);
    Task<User?> GetAdminAsync();
    Task<User?> GetManagerAsync();
}