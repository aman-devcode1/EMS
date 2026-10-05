using System.ComponentModel.DataAnnotations;

namespace EMS.Core.Entities;

public class Department : BaseEntity
{
    [Required(ErrorMessage = "Department name is required.")]
    [MaxLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}