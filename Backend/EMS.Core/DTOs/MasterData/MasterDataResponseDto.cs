namespace EMS.Core.DTOs.MasterData;

public class MasterDataResponseDto
{
    public List<DepartmentDto> Departments { get; set; } = new();
    public List<DesignationDto> Designations { get; set; } = new();
}