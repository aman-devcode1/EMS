namespace EMS.Core.DTOs.Admin;

public class AdminProfileDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? PresentAddress { get; set; }
    public DateTime? DateOfBirth { get; set; }
}