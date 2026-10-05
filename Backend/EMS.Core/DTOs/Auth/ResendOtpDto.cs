using System.ComponentModel.DataAnnotations;
using EMS.Core.Enums;

namespace EMS.Core.DTOs.Auth;

public class ResendOtpDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    public OtpPurpose Purpose { get; set; }
}