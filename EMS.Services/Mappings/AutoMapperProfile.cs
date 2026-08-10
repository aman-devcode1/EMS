using AutoMapper;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;

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
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())          // System Field
            .ForMember(dest => dest.Designation, opt => opt.Ignore())       // Register में नहीं है
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
            .ForMember(dest => dest.Department, opt => opt.Ignore())        // Register में नहीं है
            .ForMember(dest => dest.Designation, opt => opt.Ignore())       // Register में नहीं है
            .ForMember(dest => dest.Salary, opt => opt.Ignore())            // Register में नहीं है
            .ForMember(dest => dest.HireDate, opt => opt.Ignore());         // Register में नहीं है

    }
}