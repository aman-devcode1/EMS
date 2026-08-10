using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Employee;

public class EmployeeCreateDto 
{
    // Basic Details (Admin bharta hai, Lekin ye pehle se hi Register mein aa chuke hote haim, fir bhi Admin ko edit ka option hota hai.) 
    // --- Basic Identity ---
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;
    
    
    // --- Professional ---
    [Required(ErrorMessage = "Department is required.")]
    [MaxLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    public string Department { get; set; } = string.Empty;

    [Required(ErrorMessage = "Designation is required.")]
    [MaxLength(100, ErrorMessage = "Designation cannot exceed 100 characters.")]
    public string Designation { get; set; } = string.Empty;

    [Required(ErrorMessage = "Salary is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive value.")]
    public decimal Salary { get; set; } = decimal.Zero;

    [Required(ErrorMessage = "Hire Date is required.")]
    [DataType(DataType.Date, ErrorMessage = "Please provide a valid date.")]
    public DateTime HireDate { get; set; }

    // [Required(ErrorMessage = "System Role is required.")]
    // [RegularExpression("^(Admin|MAnager|Employee)$", ErrorMessage = "Invalid Role")]
    // public string? Role {get; set; } = "Employee";


    // --- Personal Deatil (Admin Edit bhi kar skta hai)---
    [Required(ErrorMessage = "Date of birth is required.")]
    [DataType(DataType.Date, ErrorMessage = "Please provide a valid date.")]
    public DateTime DateOfBirth { get; set; }

    [MaxLength(20), Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit Indian phone number starting with 6-9.")]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? PresentAddress { get; set; } // Optional (कुछ लोग नहीं देना चाहते)

    [MaxLength(200)]
    public string? PreviousCompanyRole { get; set; }

    // 🔥 🔥 NEW: UserId (Optional - अगर Employee किसी मौजूदा User से Link करना है)
    public int? UserId { get; set; } // 👈 यह Add करो!
}