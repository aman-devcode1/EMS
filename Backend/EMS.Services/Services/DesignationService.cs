using AutoMapper;
using EMS.Core.DTOs.MasterData;
using EMS.Core.Entities;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces.IRepositories;
using EMS.Core.Interfaces.IServices;

namespace EMS.Services.Services;

public class DesignationService : IDesignationService
{
    private readonly IDesignationRepository _repo;
    private readonly IMapper _mapper;

    public DesignationService(IDesignationRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<DesignationDto>> GetAllAsync(bool includeInactive = false)
    {
        var items = await _repo.GetAllAsync(includeInactive);
        return _mapper.Map<List<DesignationDto>>(items);
    }

    public async Task<DesignationDto> GetByIdAsync(int id)
    {
        var item = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Designation with Id {id} not found.");

        return _mapper.Map<DesignationDto>(item);
    }

    public async Task<DesignationDto> CreateAsync(CreateUpdateDesignationDto dto)
    {
        var existing = await _repo.GetByNameAsync(dto.Name.Trim());
        if (existing != null)
            throw new ConflictException($"Designation '{dto.Name}' already exists.");

        var entity = new Designation
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(entity);
        return _mapper.Map<DesignationDto>(entity);
    }

    public async Task<DesignationDto> UpdateAsync(int id, CreateUpdateDesignationDto dto)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Designation with Id {id} not found.");

        var existing = await _repo.GetByNameAsync(dto.Name.Trim());
        if (existing != null && existing.Id != id)
            throw new ConflictException($"Designation '{dto.Name}' already exists.");

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity);
        return _mapper.Map<DesignationDto>(entity);
    }

    public async Task<DesignationDto> ToggleActiveAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException($"Designation with Id {id} not found.");

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(entity);
        return _mapper.Map<DesignationDto>(entity);
    }
}