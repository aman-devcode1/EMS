namespace EMS.Core.DTOs.Employee;

public class EmployeeResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? PresentAddress { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? HireDate { get; set; }
    public decimal Salary { get; set; }
    public string PreviousCompanyRole { get; set; } = string.Empty;
}