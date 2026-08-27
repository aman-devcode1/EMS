using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Admin;

public class AdminRegisterDto
{
    [Required(ErrorMessage = "First name is required.") ]
    [MaxLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.") ]
    [MaxLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required.") ]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [MaxLength(10, ErrorMessage = "Phone number cannot exceed 10 digits.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password is required.") ]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8, ErrorMessageResourceName = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.") ]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}