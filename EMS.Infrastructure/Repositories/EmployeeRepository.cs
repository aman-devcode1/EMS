using EMS.Core.Common;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;
using EMS.Core.Interfaces;
using EMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // 1. GET BY ID
    // ============================================================
    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await _context.Employees
        .Include(e => e.User)   // User Data भी साथ लाओ (Role, Email)
        .FirstOrDefaultAsync(e => e.Id == id);
    }

    // ============================================================
    // 2. ADD
    // ============================================================
    public async Task AddAsync(Employee employee)
    {
        await _context.Employees.AddAsync(employee);
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // 3. UPDATE
    // ============================================================
    public async Task UpdateAsync(Employee employee)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // 3. DELETE
    // ============================================================
    public async Task DeleteAsync(int id)
    {
        var employee = await GetByIdAsync(id);
        if (employee != null)
            _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
    }

    // ============================================================
    // 6. EXISTS
    // ============================================================
    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Employees.AnyAsync(e => e.Id == id);
    }

    // ============================================================
    // 2. GET && e.Department.PAGED (Paginatin + Search + )S + Filter)
    // ============================================================
    public async Task<PagedResult<Employee>> GetPagedAsync(EmployeeQueryParams queryParams)
    {
        var query = _context.Employees.AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(queryParams.SearchTerm))
        {
            var term = queryParams.SearchTerm.ToLower();

            query = query.Where(e =>
            (e.FirstName + "" + e.LastName).ToLower().Contains(term) ||
            (e.Department != null && e.Department.ToLower().Contains(term)) ||
            (e.Email != null && e.Email.ToLower().Contains(term)) ||
            (e.Designation != null && e.Designation.ToLower().Contains(term)) ||
            (e.PhoneNumber != null && e.PhoneNumber.Contains(term))
            );
        }

        // Filter :  Department
        if (!string.IsNullOrWhiteSpace(queryParams.Department))
        {
            query = query.Where(e => e.Department == queryParams.Department);   
        }

        // Filter :  IsActive
        if (!string.IsNullOrWhiteSpace(queryParams.Designation))
        {
            query = query.Where(e => e.Designation == queryParams.Designation);
        }

        // Filter :  IsActive
        if (queryParams.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == queryParams.IsActive.Value);
        }

        // Sorting
        query = (queryParams.SortBy?.ToLower())
        switch
        {
            "firstname" => queryParams.SortDirection == "desc" ? query.OrderByDescending(e => e.FirstName) : query.OrderBy(e => e.FirstName),
            "lastname" => queryParams.SortDirection == "desc" ? query.OrderByDescending(e => e.LastName) : query.OrderBy(e => e.LastName),
            "email" => queryParams.SortDirection == "desc" ? query.OrderByDescending(e => e.Email) : query.OrderBy(e => e.Email),
            "salary" => queryParams.SortDirection == "desc" ? query.OrderByDescending(e => e.Salary) : query.OrderBy(e => e.Salary),
            "department" => queryParams.SortDirection == "desc" ? query.OrderByDescending(e => e.Department) : query.OrderBy(e => e.Department),
            "designation" => queryParams.SortDirection == "desc" ? query.OrderByDescending(e => e.Designation) : query.OrderBy(e => e.Designation),
            _ => query.OrderBy(e => e.Id)
        };

        // Pagination
        var totalRecords = await query.CountAsync();
        var items = await query
        .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
        .Take(queryParams.PageSize)
        .ToListAsync();

        return new PagedResult<Employee>
        {
          PageNumber = queryParams.PageNumber,
          PageSize = queryParams.PageSize,
          TotalRecords = totalRecords,
          TotalPages = (int)Math.Ceiling(totalRecords / (double)queryParams.PageSize),
          Items = items
        };
    }
}