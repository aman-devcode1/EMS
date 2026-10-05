using System.ComponentModel.DataAnnotations;
using EMS.Core.Enums;

namespace EMS.Core.Entities;

// 👇 Alag Table — PasswordHash/Salary jaisa hi security principle: OTP leak hone par baaki data safe rahe
public class OtpCode : BaseEntity
{
    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public OtpPurpose Purpose { get; set; }

    // Number of failed verification attempts for this OTP.
    public int FailedAttempts { get; set; }

    // Prevents an OTP from being reused after successful verification.
    public bool IsUsed { get; set; }

    // FK: User Table ko Point karta hai
    public int UserId { get; set; }

    public User? User { get; set; }
}