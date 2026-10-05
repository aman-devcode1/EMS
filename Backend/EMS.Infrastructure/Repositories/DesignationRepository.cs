using EMS.Core.Entities;
using EMS.Core.Interfaces.IRepositories;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public class DesignationRepository : IDesignationRepository
{
    private readonly AppDbContext _context;

    public DesignationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Designation>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Designations.AsQueryable();

        if (!includeInactive)
            query = query.Where(d => d.IsActive);

        return await query.OrderBy(d => d.Name).ToListAsync();
    }

    public async Task<Designation?> GetByIdAsync(int id)
    {
        return await _context.Designations.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Designation?> GetByNameAsync(string name)
    {
        return await _context.Designations
            .FirstOrDefaultAsync(d => d.Name.ToLower() == name.ToLower());
    }

    public async Task AddAsync(Designation designation)
    {
        await _context.Designations.AddAsync(designation);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Designation designation)
    {
        _context.Designations.Update(designation);
        await _context.SaveChangesAsync();
    }
}