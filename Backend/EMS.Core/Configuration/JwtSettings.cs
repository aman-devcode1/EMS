using System.Security.Principal;
using EMS.Core.Enums;

namespace EMS.Core.Configuration;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 7;

    // Admin Settings
    public int AdminAccessTokenExpiryMinutes { get; set; } = 15;
    public int AdminRefreshTokenAbsoluteHours { get; set; } = 8;
    public int AdminInactivityTimeoutMinutes { get; set; } = 5;

    // Employee Settings
    public int EmployeeAccessTokenExpiryMinutes { get; set; } = 15;
    public int EmployeeRefreshTokenAbsoluteHours { get; set; } = 24;
    public int EmployeeInactivityTimeoutMinutes { get; set; } = 60;

    public int ClockSkewSeconds { get; set; } = 5;

    // Token Pliocy
    public class TokenPolicy
    {
        public int AcessTokenExpiryMinutes { get; set; }
        public int RefreshTokenAbsoluteHours { get; set; }
        public int InactivityTimeoutMinutes { get; set; }
    }

    public TokenPolicy GetPolicyForRole(RoleType role) => role switch
    {
        RoleType.Admin or RoleType.Manager => new TokenPolicy
        {
            AcessTokenExpiryMinutes = AdminAccessTokenExpiryMinutes,
            RefreshTokenAbsoluteHours = AdminRefreshTokenAbsoluteHours,
            InactivityTimeoutMinutes = AdminInactivityTimeoutMinutes
        },
        _ => new TokenPolicy
        {
            AcessTokenExpiryMinutes = EmployeeAccessTokenExpiryMinutes,
            RefreshTokenAbsoluteHours = EmployeeRefreshTokenAbsoluteHours,
            InactivityTimeoutMinutes = EmployeeInactivityTimeoutMinutes
        }
    };
}