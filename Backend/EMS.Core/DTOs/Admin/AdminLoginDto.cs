using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Admin;

public class AdminLoginDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage = "Password must be between 8 and 100 characters."
    )]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian phone number starting with 6-9.")]
    public string PhoneNumber { get; set; } = string.Empty;
}