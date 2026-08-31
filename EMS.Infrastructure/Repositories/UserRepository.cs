using EMS.Core.Entities;
using EMS.Core.Enums;
using EMS.Core.Interfaces.IRepositories;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // 1. GET BY EMAIL (सबसे जरूरी - Login/Register में Use होगा)
    // ============================================================
    public async Task<User?> GetByEmailAsync(string email)
    {
        // 🔥 Email से User ढूँढो, Employee Data भी साथ लाओ (Navigation Property)
        return await _context.Users
        .Include(u => u.Employee)   // 👈 Employee Data भी चाहिए (जैसे Name, Salary)
        .FirstOrDefaultAsync(u => u.Email == email);
    }

    // ============================================================
    // 2. GET BY ID
    // ============================================================
    public async Task<User?> GetByIdAsync(int id)
    {
        // 🔥 Id से User ढूँढो, Employee और RefreshTokens भी साथ लाओ
        return await _context.Users
        .Include(u => u.Employee)
        .Include(u => u.RefreshTokens)
        .FirstOrDefaultAsync(u => u.Id == id);
    }

    // ============================================================
    // 3. ADD (Register)
    // ============================================================
    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // 4. UPDATE (Role Change, Password Reset)
    // ============================================================
    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // 5. DELETE (किसी User को पूरी तरह Delete करना - शायद ही Use हो)
    // ============================================================
    public async Task DeleteAsync(int id)
    {
        var user = await GetByIdAsync(id);
        if (user != null)
            _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // 6. EXISTS BY EMAIL (Duplicate Email Check)
    // ============================================================
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    // ============================================================
    // 7. GET ADMIN (Optional - पहले से है, इसे रख सकते हैं)
    // ============================================================
    public async Task<User?> GetAdminAsync()
    {
        return await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Role == RoleType.Admin);
    }
}