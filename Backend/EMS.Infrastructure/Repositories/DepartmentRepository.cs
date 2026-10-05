using EMS.Core.Entities;
using EMS.Core.Interfaces.IRepositories;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;

    public DepartmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Department>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Departments.AsQueryable();

        if (!includeInactive)
            query = query.Where(d => d.IsActive);

        return await query.OrderBy(d => d.Name).ToListAsync();
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        return await _context.Departments.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Department?> GetByNameAsync(string name)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower());
    }

    public async Task AddAsync(Department department)
    {
        await _context.Departments.AddAsync(department);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Department department)
    {
        _context.Departments.Update(department);
        await _context.SaveChangesAsync();
    }
}