using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;

public class ResendOtpDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;
}