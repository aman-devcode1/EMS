using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;

public class RegisterDto
{
    // ============================================================
    // 1. BASIC IDENTITY (पहचान) - ये Employee में भी जाएगा
    // ============================================================
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;


    // ============================================================
    // 2. LOGIN CREDENTIALS (प्रमाण-पत्र) - 🔥 यह EmployeeCreateDto में नहीं है!
    // ============================================================
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;


    // ============================================================
    // 3. PERSONAL DETAILS 
    // ============================================================
    [Phone(ErrorMessage = "Invalid phone number.")]
    [Required(ErrorMessage = "Phone number is required.")]
    [MaxLength(10, ErrorMessage = "Phone number cannot exceed 10 digits.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian phone number starting with 6-9.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(500)]
    public string? PresentAddress { get; set; } // Optional (कुछ लोग नहीं देना चाहते)

    [MaxLength(200)]
    public string? PreviousCompanyRole { get; set; }


    // ============================================================
    // 4. OPTIONAL: Employee की कुछ जरूरी Details (सिर्फ अगर चाहिए)
    // ============================================================
    // Note: हमने यहाँ Salary, Department नहीं रखे, क्योंकि हो सकता है 
    // कोई Admin खुद Register कर रहा हो (बिना Salary के)।
    // Salary/Department बाद में Admin जाकर Employee Profile में Add करेगा।
}