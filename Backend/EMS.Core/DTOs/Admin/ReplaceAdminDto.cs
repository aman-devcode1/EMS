using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Admin;

public class ReplaceAdminDto
{
    // 🔥 Current admin apna hi password confirm karega — proof ki wahi ye action kar raha hai
    [Required(ErrorMessage = "Current password is required for confirmation.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(50)]
    public string NewAdminFirstName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NewAdminLastName { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string NewAdminEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian phone number.")]
    public string NewAdminPhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(
    100,
    MinimumLength = 8,
    ErrorMessage = "Password must be between 8 and 100 characters."
)]
    [DataType(DataType.Password)]
    public string NewAdminPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare(nameof(NewAdminPassword), ErrorMessage = "Passwords do not match.")]
    public string NewAdminConfirmPassword { get; set; } = string.Empty;
}