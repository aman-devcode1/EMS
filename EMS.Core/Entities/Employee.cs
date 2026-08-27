using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Core.Entities;

public class Employee : BaseEntity
{
    // ============================================================
    // 1. BASIC IDENTITY (पहचान)
    // ============================================================
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    // 🔥 DateOfBirth: Nullable (?) रखा है, क्योंकि पुराने डेटा में हो सकता है न हो।
    [DataType(DataType.Date, ErrorMessage = "Please provide a valid date.")]
    public DateTime? DateOfBirth { get; set; }

    [MaxLength(10), Required(ErrorMessage = "PhoneNumber is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit number.")]
    public string? PhoneNumber { get; set; }

    // ============================================================
    // 2. PROFESSIONAL DETAILS (पेशेवर जानकारी)
    // ============================================================
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Previous Company Role is required.")]
    [MaxLength(200, ErrorMessage = "Previous Company Role cannot exceed 200 characters.")]
    public string PreviousCompanyRole  { get; set; } = string.Empty;

    [Required(ErrorMessage = "Designation is required.")]
    [MaxLength(100, ErrorMessage = "Designation cannot exceed 100 characters.")]
    public string Designation { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Department cannot exceed 100 characters.")]
    public string Department { get; set; } = string.Empty;

    // [Required(ErrorMessage = "Salary is required.")]
    [Column(TypeName = "decimal(18,2)")] // SQL में सटीक Decimal
    public decimal? Salary { get; set; }

    [Required(ErrorMessage = "Hire Date is required.")]
    [DataType(DataType.Date, ErrorMessage = "Please provide a valid date.")]
    public DateTime HireDate { get; set; } = DateTime.UtcNow; // Default Today

    // ============================================================
    // 3. ADDRESS (Optional)
    // ============================================================
    [MaxLength(500)]
    public string? PresentAddress { get; set; }

    // ============================================================
    // 4. RELATIONSHIP (User Table से Link) - 🔥 सबसे महत्वपूर्ण!
    // ============================================================
    // Foreign Key (FK): यह User Table की Primary Key (Id) को Point करेगा।
    public int? UserId { get; set; }
    // Normalization (डेटा सामान्यीकरण)। Login Credentials (Password) को HR Data (Salary) से अलग रखना Security के लिए बहुत जरूरी है। अगर Database Hack हो जाए, तो सिर्फ PasswordHash वाली Table जाती है, Salary वाली नहीं।   
    // Flexibility (लचीलापन)। 1. कोई User (Admin) हो सकता है जो Employee नहीं है (EmployeeId = null). 2. कोई Employee हो सकता है जिसका अभी User (Login) नहीं बना है (UserId = null).

    public User? User { get; set; }
    // Code में आसानी। हम employee.User.Role लिख सकते हैं, और EF Core SQL में JOIN करके Data लाएगा।
}
