namespace EMS.Core.DTOs.Auth;

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;     // Short-lived (15 min)
    public string RefreshToken { get; set; } = string.Empty;    // Long-lived (7 days)
    public int ExpiresIn { get; set; }     // Seconds में (e.g., 900)
    public string TokenType { get; set; } = "Bearer";    // OAuth2 standard
}