using System.ComponentModel.DataAnnotations;

namespace EMS.Core.Entities;

public abstract class BaseEntity        // Abstract मतलब इसका अपना कोई Table नहीं बनेगा। सिर्फ दूसरी Classes (Employee, User) को Properties "Inherit" (विरासत) देने के लिए। आप BaseEntity obj = new BaseEntity(); नहीं कर सकते, जो सही है क्योंकि ये कोई Real Entity नहीं है।
{
    [Key]
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    // Effect: हर Query में Where(e => e.IsActive == true) लिखना पड़ेगा। नहीं लिखा तो Deleted Employee भी दिख जाएंगे। (इसे भविष्य में Global Query Filter से ठीक करेंगे)

}

