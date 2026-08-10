using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EMS.Core.Entities;

// 👇 यह User की तरह BaseEntity से Inherit करेगा (तो Id, CreatedAt, IsActive अपने आप आ गए)
public class RefreshToken : BaseEntity
{
    // ============================================================
    // 1. THE TOKEN ITSELF (असली टोकन)
    // ============================================================
    [Required]
    [MaxLength(500)] // लंबा हो सकता है (GUID + Hashed)
    public string Token { get; set; } = string.Empty; // 👈 यह Random Unique String होगी (e.g., GUID)

    // ============================================================
    // 2. EXPIRY & REVOCATION (समाप्ति और निरस्तीकरण)
    // ============================================================
    public DateTime ExpiresAt { get; set; } // 👈 कब तक यह Token मान्य (Valid) है?

    public bool IsRevoked { get; set; } = false; // 👈 क्या इसे रद्द (Revoke) किया जा चुका है?

    public DateTime? RevokedAt { get; set; } // 👈 अगर Revoked है, तो कब किया गया?

    // ============================================================
    // 3. RELATIONSHIP (किस User से Link है)
    // ============================================================
    public int? UserId { get; set; } // 👈 Foreign Key (FK) - User Table की Id को Point करता है

    // Navigation Property (C# को बताता है कि यह Token किस User का है)
    public User? User { get; set; }
}