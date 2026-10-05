using EMS.Core.DTOs.MasterData;

namespace EMS.Core.Interfaces.IServices;

public interface IDesignationService
{
    Task<List<DesignationDto>> GetAllAsync(bool includeInactive = false);
    Task<DesignationDto> GetByIdAsync(int id);
    Task<DesignationDto> CreateAsync(CreateUpdateDesignationDto dto);
    Task<DesignationDto> UpdateAsync(int id, CreateUpdateDesignationDto dto);
    Task<DesignationDto> ToggleActiveAsync(int id);
}