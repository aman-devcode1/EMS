using System.ComponentModel.DataAnnotations;

namespace EMS.Core.DTOs.Employee;

public class UpdateEmployeeProfessionalDto
{
    [Range(10000, int.MaxValue, ErrorMessage = "Salary must be at least 10,000.")]
    public decimal? Salary { get; set; }

    public int? DepartmentId { get; set; }

    public int? DesignationId { get; set; }

    [MaxLength(200, ErrorMessage = "Previous company role cannot exceed 200 characters.")]
    public string? PreviousCompanyRole { get; set; }
}