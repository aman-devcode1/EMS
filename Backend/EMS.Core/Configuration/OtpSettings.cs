namespace EMS.Core.Configuration;

public class OtpSettings
{
    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxFailedAttempts { get; set; } = 5;
    public bool LogOtpToConsole { get; set; } = false;
}

// NUmbers are just fallback defaults. Actual values are read from appsettings.json.