using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;

public class InitiateLoginDto
{
    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(
    100,
    MinimumLength = 8,
    ErrorMessage = "Password must be between 8 and 100 characters."
)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}