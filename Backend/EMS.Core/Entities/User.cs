using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EMS.Core.Enums;

namespace EMS.Core.Entities;

public class User : BaseEntity
{
    // ============================================================
    // 1. CREDENTIALS (प्रमाण-पत्र)
    // ============================================================
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    // 🔥 PasswordHash: Plain Password नहीं, बल्कि Hashed (BCrypt/SHA256) स्टोर होगा।
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    // ============================================================
    // 2. PERMISSIONS (अधिकार)
    // ============================================================
    // Enum से Role लेंगे। डिफॉल्ट "Employee" होगा।
    public RoleType Role { get; set; } = RoleType.Employee;
        
    public bool IsTwoFactorEnabled { get; set; } = false; // Employee अपनी Profile से Enable कर सकता है.

// ============================================================
    // 3. FOREIGN KEYS (Relationships) - 🔥 ये Missing थे!
    // ============================================================
    // 🔥 FK: User → Employee (One-to-One)
    // यह Employee Table की Id को Point करता है
    // public int? EmployeeId { get; set; }

    // ============================================================
    // 4. RELATIONSHIP - NAVIGATION PROPERTIES 
    // ============================================================
    // Navigation Property: C# को बताता है कि इस User का 1 Employee होगा।
    // 🔥 User → Employee (One-to-One) - Reverse Side
    // [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    // 🔥 User → RefreshTokens (One-to-Many)
    // एक User के Multiple (कई) Refresh Tokens हो सकते हैं
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    // ICollection<RefreshToken> (Multiple Tokens)
}