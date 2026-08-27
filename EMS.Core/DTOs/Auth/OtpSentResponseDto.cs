namespace EMS.Core.Dtos.Auth;

public class OtpSentResponseDto
{
    public string Email { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
}