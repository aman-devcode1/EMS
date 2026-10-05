using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;

public class ConfirmLoginDto
{
    [Required(ErrorMessage = "TempToken is required.")]
    public string TempToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required."), EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OtpCode is required."), StringLength(6, MinimumLength = 6, ErrorMessage = "OtpCode must be exactly 6 characters.")]
    public string OtpCode { get; set; } = string.Empty;
}