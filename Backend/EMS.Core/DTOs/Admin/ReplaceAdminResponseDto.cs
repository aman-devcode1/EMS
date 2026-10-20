namespace EMS.Core.DTOs.Admin;

public class ReplaceAdminResponseDto
{
    // Phase 1 response
    public bool RequiresApproval { get; set; }

    // Phase 2 response
    public string? NextAdminEmail { get; set; }
}