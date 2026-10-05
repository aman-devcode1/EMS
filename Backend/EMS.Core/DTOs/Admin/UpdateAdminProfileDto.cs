using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Admin;

public class UpdateAdminProfileDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[6-9]\d{9}$")]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? PresentAddress { get; set; }

    public DateTime? DateOfBirth { get; set; }
}