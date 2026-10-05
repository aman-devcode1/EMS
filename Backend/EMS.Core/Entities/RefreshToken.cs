using System.ComponentModel.DataAnnotations;

namespace EMS.Core.Entities;

public class RefreshToken : BaseEntity
{
    // SHA256 hash of raw token (fast lookup)
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    // iske baad renewal nahi
    public DateTime AbsoluteExpiresAt { get; set; }

    // Inactivity tracking
    public DateTime LastUsedAt { get; set; }

    // Rotation/reuse chain
    public string? ReplacedByTokenHash { get; set; }

    // Revocation audit
    public string? RevokedReason { get; set; }
    public string? RevokedByIp { get; set; }

    // Creation audit
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
    public string? Jti { get; set; }

    // ============================================================
    // EXISTING FIELDS (backward compatible — mat hatao)
    // ============================================================
    public DateTime ExpiresAt { get; set; }       // Legacy — AbsoluteExpiresAt use karo naye code mein
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }
}