using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;

public class InitiateLoginDto
{
    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required."), MaxLength(6, ErrorMessage = "Password cannot exceed 6 characters.")]
    public string Password { get; set; } = string.Empty;
}