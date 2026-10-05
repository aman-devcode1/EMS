namespace EMS.Core.DTOs.Auth;

public class ResetPasswordResponseDto
{
    // "Admin" | "Manager" | "Employee"
    // Safe to return here because user proved email ownership via OTP.
    public string Role { get; set; } = string.Empty;
}