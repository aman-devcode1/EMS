using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;

public class VerifyOtpDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be exactly 6 digits.")]
    public string Code { get; set; } = string.Empty;
}