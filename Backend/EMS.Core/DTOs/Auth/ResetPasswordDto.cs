using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;
public class ResetPasswordDto
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Otp is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Otp must be exactly 6-digits.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(
        100,
        MinimumLength = 8,
        ErrorMessage = "Password must be between 8 and 100 characters."
    )]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", ErrorMessage = "Password must contain uppercase, lowercase, digit, and special character.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "New password and confirm password do not match."
    )]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}