using AutoMapper;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;
using EMS.Core.DTOs.MasterData;

namespace EMS.Services.Mapping;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // ============================================================
        // RegisterDto → User
        // ============================================================
        CreateMap<RegisterDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())                // Auto-Generate
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())      // Manual Hash
            .ForMember(dest => dest.Role, opt => opt.Ignore())              // Manual Set
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());         // System Field

        // ============================================================
        // RegisterDto → Employee
        // ============================================================
        CreateMap<RegisterDto, Employee>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())                // Auto-Generate
            .ForMember(dest => dest.UserId, opt => opt.Ignore())            // User Create के बाद Set होगा
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())          // System Field\
            .ForMember(dest => dest.Salary, opt => opt.Ignore())            // Register में नहीं है
            .ForMember(dest => dest.HireDate, opt => opt.Ignore());         // Register में नहीं है

        // ============================================================
        // Admin RegisterDto → User
        // ============================================================
        CreateMap<AdminRegisterDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())                // Auto-Generate
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())      // Manual Hash
            .ForMember(dest => dest.Role, opt => opt.Ignore())              // Manual Set
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());         // System Field

        // ============================================================
        //  Admin RegisterDto → Employee
        // ============================================================
        CreateMap<AdminRegisterDto, Employee>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())                // Auto-Generate
            .ForMember(dest => dest.UserId, opt => opt.Ignore())            // User Create के बाद Set होगा
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())         // System Field
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())          // System Field
            .ForMember(dest => dest.Salary, opt => opt.Ignore())            // Register में नहीं है
            .ForMember(dest => dest.HireDate, opt => opt.Ignore());         // Register में नहीं है

        // ============================================================
        //  Employee → EmployeeResponseDto
        // ============================================================
        CreateMap<Employee, EmployeeResponseDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
        .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId ?? 0))
        .ForMember(dest => dest.Department, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
        .ForMember(dest => dest.Designation, opt => opt.MapFrom(src => src.Designation != null ? src.Designation.Name : null))
        .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.User != null ? src.User.Role.ToString() : ""))
        .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
        .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // ============================================================
        //  EmployeeCreateDto → Employee
        // ============================================================
        CreateMap<EmployeeCreateDto, Employee>()
        .ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.UserId, opt => opt.Ignore())
        .ForMember(dest => dest.User, opt => opt.Ignore())
        .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
        .ForMember(dest => dest.IsActive, opt => opt.Ignore());

        // ============================================================
        // ReplaceAdminDto → User
        // ============================================================
        CreateMap<ReplaceAdminDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.NewAdminEmail))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())      // Manual Hash
            .ForMember(dest => dest.Role, opt => opt.Ignore())              // Manual Set
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());

        // ============================================================
        // ReplaceAdminDto → Employee
        // ============================================================
        CreateMap<ReplaceAdminDto, Employee>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.NewAdminFirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.NewAdminLastName) ? null : src.NewAdminLastName.Trim()))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.NewAdminEmail))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.NewAdminPhoneNumber))
            .ForMember(dest => dest.UserId, opt => opt.Ignore())            // User Create ke baad Set hoga
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Salary, opt => opt.Ignore())
            .ForMember(dest => dest.HireDate, opt => opt.Ignore())
            .ForMember(dest => dest.PresentAddress, opt => opt.Ignore())
            .ForMember(dest => dest.PreviousCompanyRole, opt => opt.Ignore())
            .ForMember(dest => dest.ScheduledForDeletionAt, opt => opt.Ignore());

        // ============================================================
        // Department ↔ DepartmentDto
        // ============================================================
        CreateMap<Department, DepartmentDto>();

        // ============================================================
        // Designation ↔ DesignationDto
        // ============================================================
        CreateMap<Designation, DesignationDto>();
    }
}