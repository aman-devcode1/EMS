namespace EMS.Core.DTOs.Employee;

public class EmployeeResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string? Department { get; set; }
    public int? DesignationId { get; set; }
    public string? Designation { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PresentAddress { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal Salary { get; set; }
    public string PreviousCompanyRole { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}