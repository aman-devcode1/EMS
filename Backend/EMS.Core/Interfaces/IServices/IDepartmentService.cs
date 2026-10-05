using EMS.Core.DTOs.MasterData;

namespace EMS.Core.Interfaces.IServices;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync(bool includeInactive = false);
    Task<DepartmentDto> GetByIdAsync(int id);
    Task<DepartmentDto> CreateAsync(CreateUpdateDepartmentDto dto);
    Task<DepartmentDto> UpdateAsync(int id, CreateUpdateDepartmentDto dto);
    Task<DepartmentDto> ToggleActiveAsync(int id);
}