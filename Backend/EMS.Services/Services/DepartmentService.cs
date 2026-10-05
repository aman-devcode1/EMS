using AutoMapper;
using EMS.Core.DTOs.MasterData;
using EMS.Core.Entities;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.IRepositories;
using EMS.Core.Interfaces.IServices;

namespace EMS.Services.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;
    private readonly IMapper _mapper;

    public DepartmentService(IDepartmentRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<DepartmentDto>> GetAllAsync(bool includeInactive = false)
    {
        var items = await _repo.GetAllAsync(includeInactive);
        return _mapper.Map<List<DepartmentDto>>(items);
    }

    public async Task<DepartmentDto> GetByIdAsync(int id)
    {
        var item = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Department with Id {id} not found.");

        return _mapper.Map<DepartmentDto>(item);
    }

    public async Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto dto)
    {
        var existing = await _repo.GetByNameAsync(dto.Name.Trim());
        if (existing != null)
            throw new ConflictException($"Department '{dto.Name}' already exists.");

        var entity = new Department
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        return _mapper.Map<DepartmentDto>(entity);
    }

    public async Task<DepartmentDto> UpdateAsync(int id, CreateUpdateDepartmentDto dto)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Department with Id {id} not found.");

        var existing = await _repo.GetByNameAsync(dto.Name.Trim());
        if (existing != null && existing.Id != id)
            throw new ConflictException($"Department '{dto.Name}' already exists.");

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity);
        return _mapper.Map<DepartmentDto>(entity);
    }

    public async Task<DepartmentDto> ToggleActiveAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Department with Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity);
        return _mapper.Map<DepartmentDto>(entity);
    }
}