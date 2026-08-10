using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Auth;

public class RefreshRequestDto
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}