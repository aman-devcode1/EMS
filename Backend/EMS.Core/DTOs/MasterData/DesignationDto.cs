using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.MasterData;

// Response
public class DesignationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Create / Update
public class CreateUpdateDesignationDto
{
    [Required(ErrorMessage = "Designation name is required.")]
    [MaxLength(100, ErrorMessage = "Designation name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}