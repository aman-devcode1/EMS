using System.ComponentModel.DataAnnotations;

namespace EMS.Core.Entities;

public class Designation : BaseEntity
{
    [Required(ErrorMessage = "Designation name is required.")]
    [MaxLength(100, ErrorMessage = "Designation name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}