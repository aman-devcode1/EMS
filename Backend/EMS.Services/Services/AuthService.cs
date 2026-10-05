using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;
using EMS.Core.Entities;
using EMS.Core.Enums;
using EMS.Core.Exceptions;
using EMS.Core.Interfaces;
using EMS.Core.Interfaces.IRepositories;
using EMS.Core.Interfaces.IServices;
using EMS.Core.Interfaces.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using EMS.Core.Dtos.Auth;
using EMS.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace EMS.Services.Services;

public class AuthService : IAuthService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOtpRepository _otpRepository;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IMapper _mapper;

    public AuthService(
        IRefreshTokenRepository refreshTokenRepository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IOtpRepository otpRepository,
        IOtpService otpService,
        IEmailService emailService,
        IConfiguration configuration,
        IOptions<JwtSettings> jwtSettings,
        ILogger<AuthService> logger,
        IMapper mapper
        )
    {
        _refreshTokenRepository = refreshTokenRepository;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _otpService = otpService;
        _emailService = emailService;
        _configuration = configuration;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
        _mapper = mapper;
    }


    // ============================================================
    // COMMON: Token Issuance — sabke liye refresh token 1 din - role based TTL
    // ============================================================
    private async Task<TokenResponseDto> IssueTokensAsync(User user, string? ip = null, string? userAgent = null)
    {
        var policy = _jwtSettings.GetPolicyForRole(user.Role);
        var accessToken = GenerateAccessToken(user, policy.AcessTokenExpiryMinutes);
        var (rawRefresh, refreshHash) = GenerateRefreshToken();

        // Revoke all previous session (single active session policy)
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, "New Login", ip);

        var absoluteExpiry = DateTime.UtcNow.AddHours(policy.RefreshTokenAbsoluteHours);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            AbsoluteExpiresAt = absoluteExpiry,
            ExpiresAt = absoluteExpiry,
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedByIp = ip,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            IsRevoked = false
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = rawRefresh,
            ExpiresIn = policy.AcessTokenExpiryMinutes * 60, // seconds
            TokenType = "Bearer"
        };
    }

    // ============================================================
    // 1. REGISTER (Employee) — OTP jayega, tokens turant nahi
    // ============================================================
    public async Task<OtpSentResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
        if (existingUser != null)
            throw new ConflictException("This email is already registered. Please sign in instead.");

        var existingEmployee = await _employeeRepository.GetByEmailAsync(registerDto.Email);
        if (existingEmployee != null)
            throw new ConflictException("This email is already associated with an employee record. Please use another email or contact support.");

        var today = DateTime.UtcNow;
        var minAllowedYear = today.Year - 100;
        var maxAllowedYear = today.Year - 18;

        if (registerDto.DateOfBirth.Year < minAllowedYear)
            throw new BadRequestException($"Date of birth year must be {minAllowedYear} or later.");

        if (registerDto.DateOfBirth.Year > maxAllowedYear)
            throw new BadRequestException("You must be at least 18 years old to register.");

        var user = _mapper.Map<User>(registerDto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
        user.Role = RoleType.Employee;
        user.IsActive = false; // OTP verify hone tak inactive
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.AddAsync(user);

        var employee = _mapper.Map<Employee>(registerDto);
        employee.UserId = user.Id;
        employee.IsActive = false;
        employee.CreatedAt = DateTime.UtcNow;
        employee.PresentAddress ??= string.Empty;
        employee.PreviousCompanyRole ??= string.Empty;

        await _employeeRepository.AddAsync(employee);

        await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email, OtpPurpose.Registration);
        return new OtpSentResponseDto { Email = user.Email };
    }

    // ============================================================
    // 2. LOGIN (Employee) — Direct, OTP nahi
    // ============================================================
    public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent email: {Email}", loginDto.Email);
            throw new UnauthorizedAccessException(
                "No account found with this email. Please check or register.");
        }

        if (!VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt (wrong password) for {Email}", loginDto.Email);
            throw new UnauthorizedAccessException(
                "Incorrect password. If you recently changed it, please use your new password.");
        }

        // Manager/Admin ko /api/Admin/login (OTP wala) use karna hi hoga.
        if (user.Role != RoleType.Employee)
            throw new UnauthorizedAccessException("This account is not an employee account. Please use the Admin/Manager sign-in.");

        if (user.Employee == null || user.Employee.PhoneNumber != loginDto.PhoneNumber)
            throw new UnauthorizedAccessException("The phone number doesn't match our records. Please verify and try again.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account is not verified yet. Please complete OTP verification.");

        _logger.LogInformation("Employee login success: UserId={UserId}, Email={Email}", user.Id, user.Email);
        return await IssueTokensAsync(user);
    }

    // ============================================================
    // 3. REFRESH TOKEN - rotation + reuse detection + timeouts
    // ============================================================
    public async Task<TokenResponseDto> RefreshTokenAsync(string? rawToken, string? ip = null, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new UnauthorizedAccessException("Session expired. Please login again.");

        var tokenHash = HashToken(rawToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        // Reuse Detection: Token not found - could be a rotated token being used.
        if (storedToken == null)
        {
            _logger.LogWarning("Refresh attempt with unknown token. IP={IP}, UA={UserAgent}", ip, userAgent);
            // We cannot identify the user here, so just reject
            throw new UnauthorizedAccessException("Refresh token not found.");
        }

        // Absolute Expiry
        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
             _logger.LogInformation("Refresh token absolute expiry for UserId={UserId}", storedToken.UserId);
            await _refreshTokenRepository.RevokeAsync(storedToken, "Absolute expiry", ip);
            throw new UnauthorizedAccessException("Refresh token has expired. Please login again.");
        }

        // If token was already revoked (rotated/logged out), this is a REUSE ATTACK
        if (storedToken.IsRevoked)
        {
            _logger.LogCritical("SECURITY ALERT: Refresh token REUSE detected! UserId={UserId}, IP={IP}, OriginalRevokeReason={Reason}. All sessions revoked.", storedToken.UserId, ip, storedToken.RevokedReason);
            await _refreshTokenRepository.RevokeAllByUserIdAsync(storedToken.UserId ?? 0, "Reuse detected - possible theft", ip);
            throw new UnauthorizedAccessException("Security alert: Your session was terminated. Please login again.");
            // throw new UnauthorizedAccessException("Refresh token has been revoked. Please login again.");
        }

        var user = storedToken.User ?? throw new UnauthorizedAccessException("User not found for the provided refresh token.");
        var policy = _jwtSettings.GetPolicyForRole(user.Role);

        // Inactivity
        if (DateTime.UtcNow - storedToken.LastUsedAt > TimeSpan.FromMinutes(policy.InactivityTimeoutMinutes))
        {
            _logger.LogInformation("Session expired due to inactivity: UserId={UserId}", user.Id);
            await _refreshTokenRepository.RevokeAsync(storedToken, "Inactivity timeout", ip);
            throw new UnauthorizedAccessException("Session expired due to inactivity. Please login again.");
        }

        // Rotate: issue new pair, mark old as rotaed (with chain refrence)
        var accessToken = GenerateAccessToken(user, policy.AcessTokenExpiryMinutes);
        var (newRaw, newHash) = GenerateRefreshToken();

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedReason = "Rotated";
        storedToken.RevokedByIp = ip;
        storedToken.ReplacedByTokenHash = newHash;

        await _refreshTokenRepository.UpdateAsync(storedToken);

        var newToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newHash,
            AbsoluteExpiresAt = storedToken.AbsoluteExpiresAt, // SAME absolute limit — do NOT extend
            ExpiresAt = storedToken.AbsoluteExpiresAt,
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip,
            UserAgent = userAgent?.Substring(0, Math.Min(userAgent.Length, 500)),
            IsRevoked = false
        };
        await _refreshTokenRepository.CreateAsync(newToken);

_logger.LogDebug("Token rotated for UserId={UserId}", user.Id);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRaw,
            ExpiresIn = policy.AcessTokenExpiryMinutes * 60,
            TokenType = "Bearer"
        };
    }

    // ============================================================
    // 4. REVOKE (Logout)
    // ============================================================
    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
        if (storedToken == null)
            return false;

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.RevokedReason = "Logout";
        await _refreshTokenRepository.UpdateAsync(storedToken);

        _logger.LogInformation("Logout: UserId={UserId}", storedToken.UserId);
        return true;
    }

    // ============================================================
    // 5. REVOKE ALL TOKENS
    // ============================================================
    public async Task<bool> RevokeAllTokensAsync(int userId)
    {
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
        return true;
    }

    // ============================================================
    // 6. ADMIN REGISTER — OTP jayega
    // ============================================================
    public async Task<OtpSentResponseDto> AdminRegisterAsync(AdminRegisterDto adminRegisterDto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(adminRegisterDto.Email);

        if (existingUser != null)
        {
            if (existingUser.IsActive)
            {
                // Verified account hai — dobara register nahi hone dena
                throw new ConflictException("This email is already registered and verified. Please login instead.");
            }

            // Unverified account hai — naya record banane ke bajaye, purane ko hi
            // dobara OTP bhej do. Frontend ka success-handler already OTP verify
            // page par navigate kar deta hai — koi extra button/logic nahi chahiye.
            await _otpService.GenerateAndSendOtpAsync(existingUser.Id, existingUser.Email, OtpPurpose.Registration);
            return new OtpSentResponseDto
            {
                Email = existingUser.Email
            };
        }

        var existingAdmin = await _userRepository.GetAdminAsync();
        if (existingAdmin != null)
            throw new ConflictException("An admin already exists. Only one admin is allowed.");

        if (adminRegisterDto.Password != adminRegisterDto.ConfirmPassword)
            throw new BadRequestException("Password and Confirm Password do not match. Please re-enter them.");

        var user = _mapper.Map<User>(adminRegisterDto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminRegisterDto.Password);
        user.Role = RoleType.Admin;
        user.IsActive = false;
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.AddAsync(user);

        var employee = _mapper.Map<Employee>(adminRegisterDto);
        employee.UserId = user.Id;
        employee.IsActive = false;
        employee.CreatedAt = DateTime.UtcNow;

        await _employeeRepository.AddAsync(employee);

        await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email, OtpPurpose.Registration);
        return new OtpSentResponseDto { Email = user.Email };
    }

    // ============================================================
    // 7. ADMIN / MANAGER LOGIN — Step 1: Password+Phone check → OTP bhejo
    // ============================================================
    public async Task<OtpSentResponseDto> AdminLoginAsync(AdminLoginDto adminLoginDto)
    {
        var user = await _userRepository.GetByEmailAsync(adminLoginDto.Email);
        if (user == null)
        {
            _logger.LogWarning("Admin login attempt for non-existent email: {Email}", adminLoginDto.Email);
            throw new UnauthorizedAccessException(
                "No account found with this email. Please check the email or register a new account.");
        }

        if (user.Role != RoleType.Admin && user.Role != RoleType.Manager)
            throw new UnauthorizedAccessException("This is not an admin account. Please use the Employee sign-in instead.");

        if (!VerifyPassword(adminLoginDto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed admin login attempt (wrong password) for {Email}", adminLoginDto.Email);
            throw new UnauthorizedAccessException("Incorrect password. If you recently changed it, please use your new password.");
        }

        if (user.Employee == null)
            throw new UnauthorizedAccessException("Your employee profile is missing. Please contact support.");

        if (user.Employee.PhoneNumber != adminLoginDto.PhoneNumber)
            throw new UnauthorizedAccessException("The phone number doesn't match our records. Please verify and try again.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Your account is not verified yet. Please complete OTP verification.");

_logger.LogInformation("Admin OTP sent: UserId={UserId}, Email={Email}, Role={Role}", user.Id, user.Email, user.Role);
        await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email, OtpPurpose.Login);
        return new OtpSentResponseDto { Email = user.Email };
    }

    // ============================================================
    // 8. VERIFY REGISTRATION OTP (Employee + Admin dono)
    // ============================================================
    public async Task<TokenResponseDto> VerifyRegistrationOtpAsync(VerifyOtpDto verifyOtpDto)
    {
        // CHECK 1: User exists?
        var user = await _userRepository.GetByEmailAsync(verifyOtpDto.Email);
        if (user == null)
            throw new NotFoundException("No account found with this email. Please register first.");

        // CHECK 2: OTP exists?
        var otp = await _otpRepository.GetOtpCodeByUserIdAsync(user.Id);
        if (otp == null)
            throw new BadRequestException("No OTP found. Please request a new one.");

        // CHECK 3: OTP purpose correct?
        if (otp.Purpose != OtpPurpose.Registration)
            throw new BadRequestException("This OTP is not for Registration. Please use the correct otp.");

        // CHECK 4: OTP still active?
        if (!otp.IsActive)
            throw new BadRequestException("This OTP is no longer active. Please request a new one.");

        // CHECK 5: OTP expired?
        if (otp.ExpiresAt <= DateTime.UtcNow)
            throw new BadRequestException("Your OTP has expired. Please request a new one.");

        // CHECK 6: OTP code matches?
        if (!string.Equals(otp.Code, verifyOtpDto.Code.Trim(), StringComparison.Ordinal))
            throw new BadRequestException("Incorrect OTP. Please check the OTP and try again.");

        await _otpRepository.DeleteOtpCodeAsync(otp.Id);

        user.IsActive = true;
        await _userRepository.UpdateAsync(user);

        if (user.Employee != null)
        {
            user.Employee.IsActive = true;
            await _employeeRepository.UpdateAsync(user.Employee);
        }

        return await IssueTokensAsync(user);
    }

    // ============================================================
    // 9. VERIFY ADMIN/MANAGER LOGIN OTP — Step 2: Tokens milenge
    // ============================================================
    public async Task<TokenResponseDto> VerifyAdminLoginOtpAsync(VerifyOtpDto verifyOtpDto)
    {
        // CHECK 1: User exists?
        var user = await _userRepository.GetByEmailAsync(verifyOtpDto.Email);
        if (user == null)
            throw new UnauthorizedAccessException(
                "No account found with this email. Please sign in again.");

        // CHECK 2: OTP exists?
        var otp = await _otpRepository.GetOtpCodeByUserIdAsync(user.Id);
        if (otp == null)
            throw new BadRequestException(
                "No OTP found. Please sign in again to receive a new one.");

        // CHECK 3: OTP purpose correct?
        if (otp.Purpose != OtpPurpose.Login)
            throw new BadRequestException(
                "This OTP is not for login. Please use the correct OTP.");

        // CHECK 4: OTP still active?
        if (!otp.IsActive)
            throw new BadRequestException(
                "This OTP is no longer active. Please sign in again.");

        // CHECK 5: OTP expired?
        if (otp.ExpiresAt <= DateTime.UtcNow)
            throw new BadRequestException(
                "Your OTP has expired. Please sign in again to receive a new one.");

        // CHECK 6: OTP code matches?
        if (!string.Equals(otp.Code, verifyOtpDto.Code.Trim(), StringComparison.Ordinal))
        {
            _logger.LogWarning("Invalid OTP attempt for {Email} (admin login)", verifyOtpDto.Email);
            throw new BadRequestException(
                "Incorrect OTP. Please check the code and try again.");
        }

        await _otpRepository.DeleteOtpCodeAsync(otp.Id);

        _logger.LogInformation("Admin login success: UserId={UserId}, Email={Email}, Role={Role}", user.Id, user.Email, user.Role);

        return await IssueTokensAsync(user);
    }

    // ============================================================
    // 10. Resend Otp
    // ============================================================
    public async Task<OtpSentResponseDto> ResendOtpAsync(ResendOtpDto resendOtpDto)
    {
        // CHECK 1: User exists?
        var user = await _userRepository.GetByEmailAsync(resendOtpDto.Email);
        if (user == null)
            throw new NotFoundException(
                "No account found with this email. Please register first.");

        // CHECK 2: Purpose validation
        if (resendOtpDto.Purpose == OtpPurpose.Registration)
        {
            if (user.IsActive)
                throw new ConflictException(
                    "This account is already verified. Please sign in instead.");
        }
        else if (resendOtpDto.Purpose == OtpPurpose.Login)
        {
            if (!user.IsActive)
                throw new ConflictException(
                    "This account is not verified yet. Please complete registration first.");

            if (user.Role != RoleType.Admin && user.Role != RoleType.Manager)
                throw new UnauthorizedAccessException(
                    "OTP login is only available for Admin and Manager accounts. Please use the regular sign-in.");
        }
        else
        {
            throw new BadRequestException(
                "Invalid OTP purpose. Please try again.");
        }

        await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email, resendOtpDto.Purpose);
        return new OtpSentResponseDto { Email = user.Email };
    }

    // ============================================================
    // 11. REPLACE ADMIN — Sirf current Admin hi kar sakta hai
    // ============================================================
    public async Task<OtpSentResponseDto> ReplaceAdminAsync(int currentAdminUserId, ReplaceAdminDto replaceAdminDto)
    {
        // CHECK 1: Current admin exists?
        var currentAdmin = await _userRepository.GetByIdAsync(currentAdminUserId);
        if (currentAdmin == null)
            throw new NotFoundException(
                "Your session is no longer valid. Please sign in again.");

        // CHECK 2: Current password correct?
        if (!VerifyPassword(replaceAdminDto.CurrentPassword, currentAdmin.PasswordHash))
            throw new UnauthorizedAccessException(
                "Your current password is incorrect. Please try again.");

        // CHECK 3: New email already taken?
        var existingUser = await _userRepository.GetByEmailAsync(replaceAdminDto.NewAdminEmail);
        if (existingUser != null)
        {
            if (existingUser.IsActive)
                throw new ConflictException(
                    "This email is already registered and verified. Please use a different email.");

            // Purana unverified record delete करो — fresh बनाएंगे
            await _userRepository.DeleteAsync(existingUser.Id);
        }

        var newAdminUser = _mapper.Map<User>(replaceAdminDto);
        newAdminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(replaceAdminDto.NewAdminPassword);
        newAdminUser.Role = RoleType.Admin;
        newAdminUser.IsActive = false;
        newAdminUser.CreatedAt = DateTime.UtcNow;
        await _userRepository.AddAsync(newAdminUser);

        var newAdminEmployee = _mapper.Map<Employee>(replaceAdminDto);
        newAdminEmployee.UserId = newAdminUser.Id;
        newAdminEmployee.IsActive = false;
        newAdminEmployee.CreatedAt = DateTime.UtcNow;
        await _employeeRepository.AddAsync(newAdminEmployee);

        // 🔥 Purane Admin ko demote karo — DELETE nahi, sirf role change + 30-din ka timer
        currentAdmin.Role = RoleType.Employee;
        await _userRepository.UpdateAsync(currentAdmin);

        if (currentAdmin.Employee != null)
        {
            currentAdmin.Employee.ScheduledForDeletionAt = DateTime.UtcNow.AddDays(30);
            await _employeeRepository.UpdateAsync(currentAdmin.Employee);
        }

        // 🔥 Purane Admin ke saare sessions turant invalid karo (jaisa tumne kaha tha)
        await _refreshTokenRepository.RevokeAllByUserIdAsync(currentAdmin.Id, "Admin replaced");

        _logger.LogCritical("ADMIN REPLACED: OldAdminId={OldAdminId}, NewAdminEmail={NewAdminEmail}", currentAdmin.Id, newAdminUser.Email);

        // User row poori tarah delete — login access turant, permanently khatam.
        // FK "SetNull" configured hai, isliye Employee row safe rahegi, sirf UserId = NULL ho jaayega.
        await _userRepository.DeleteAsync(currentAdmin.Id);

        // Naye admin ko OTP bhejo — wahi purana mechanism reuse ho raha hai
        await _otpService.GenerateAndSendOtpAsync(newAdminUser.Id, newAdminUser.Email, OtpPurpose.Registration);
        return new OtpSentResponseDto { Email = newAdminUser.Email };
    }

    // ============================================================
    // 12. FORGET PASSWORD — sabke liye ek hi generic endpoint (email se hi user dhundha jata hai, role check karne ki koi zarurat nahi).
    // ============================================================
    public async Task<OtpSentResponseDto> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        var genericResponse = new OtpSentResponseDto
        {
            Email = forgotPasswordDto.Email,
        };

        var user = await _userRepository.GetByEmailAsync(forgotPasswordDto.Email);

        if (user == null || !user.IsActive) return genericResponse;

        try
        {
            await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email, OtpPurpose.PasswordReset);
        }
        catch (ExternalServiceException ex)
        {
            // 🔥 Security: swallow — वरना response time/status से पता चलेगा कि email exists
            Console.WriteLine($"⚠️ Forgot-password email failed for {forgotPasswordDto.Email}: {ex.Message}");
        }
        return genericResponse;
    }

    // ============================================================
    // 13. RESET PASSWORD — email -> otp -> password.
    // ============================================================
    public async Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        // CHECK 1: User exists?
        var user = await _userRepository.GetByEmailAsync(resetPasswordDto.Email);
        if (user == null)
            throw new BadRequestException(
                "Invalid or expired reset code. Please request a new one.");

        // CHECK 2: Account active?
        if (!user.IsActive)
            throw new BadRequestException(
                "Your account is not verified. Please complete registration first.");

        // CHECK 3: OTP exists?
        var otp = await _otpRepository.GetOtpCodeByUserIdAsync(user.Id);
        if (otp == null)
            throw new BadRequestException(
                "No reset code found. Please request a new one.");

        // CHECK 4: OTP purpose correct?
        if (otp.Purpose != OtpPurpose.PasswordReset)
            throw new BadRequestException(
                "This code is not for password reset. Please request a new one.");

        // CHECK 5: OTP already used?
        if (otp.IsUsed)
            throw new BadRequestException(
                "This reset code has already been used. Please request a new one.");

        // CHECK 6: OTP expired?
        if (otp.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException(
                "Your reset code has expired. Please request a new one.");

        // Normalize code (spaces/special chars hata do)
        var submittedCode = new string(
            (resetPasswordDto.Code ?? string.Empty)
                .Where(char.IsDigit)
                .ToArray());

        // CHECK 7: Code matches?
        if (!string.Equals(otp.Code, submittedCode, StringComparison.Ordinal))
        {
            otp.FailedAttempts++;
            if (otp.FailedAttempts >= 5)
                await _otpRepository.DeleteOtpCodeAsync(otp.Id);
            else
                await _otpRepository.UpdateOtpCodeAsync(otp);

            throw new BadRequestException(
                "Incorrect reset code. Please check the code and try again.");
        }

        // CHECK 8: New password different from old?
        if (VerifyPassword(resetPasswordDto.NewPassword, user.PasswordHash))
            throw new BadRequestException(
                "New password must be different from your current password.");

        // ✅ All good
        await _otpRepository.DeleteOtpCodeAsync(otp.Id);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
        await _userRepository.UpdateAsync(user);
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, "Password reset");

        _logger.LogInformation("Password reset: UserId={UserId}, Email={Email}", user.Id, user.Email);

        try { await _emailService.SendPasswordChangedEmailAsync(user.Email); }
        catch { }

        return new ResetPasswordResponseDto { Role = user.Role.ToString() };
    }

    // ============================================================
    // 14. GET CURRENT COMPANY MANAGER (unique)
    // ============================================================
    public async Task<int?> GetManagerUserIdAsync()
    {
        var manager = await _userRepository.GetManagerAsync();
        return manager?.Id;
    }

    // ============================================================
    // 15. SET MANAGER ROLE (idempotent)
    // ============================================================
    public async Task<bool> SetManagerRoleAsync(int userId, bool isManager)
    {
        // CHECK 1: User exists?
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException(
                "Employee not found.");

        // CHECK 2: Account verified?
        if (!user.IsActive)
            throw new BadRequestException(
                "This employee's account is not verified yet. They must complete OTP verification first.");

        if (isManager)
        {
            // CHECK 3: Already a manager? (idempotent)
            if (user.Role == RoleType.Manager) return true;

            // CHECK 4: Only employee can be promoted?
            if (user.Role != RoleType.Employee)
                throw new ConflictException(
                    "Only employees can be promoted to Manager.");

            // CHECK 5: Another manager already exists?
            var existingManager = await _userRepository.GetManagerAsync();
            if (existingManager != null)
                throw new ConflictException(
                    "A company manager already exists. Please remove the current manager first.");

            user.Role = RoleType.Manager;
        }
        else
        {
            // CHECK 6: Not a manager? (idempotent)
            if (user.Role != RoleType.Manager) return true;
            user.Role = RoleType.Employee;
        }

        await _userRepository.UpdateAsync(user);
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, "Role changed");

        _logger.LogWarning("Role changed: UserId={UserId}, NewRole={Role}", user.Id, user.Role);

        return true;
    }

    // ============================================================
    // 16. GET ADMIN PROFILE
    // ============================================================
    public async Task<AdminProfileDto> GetAdminProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");

        var employee = await _employeeRepository.GetByUserIdAsync(userId)
            ?? throw new NotFoundException("Employee record not found.");

        return new AdminProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            PhoneNumber = employee.PhoneNumber ?? string.Empty,
            PresentAddress = employee.PresentAddress,
            DateOfBirth = employee.DateOfBirth
        };
    }

    // ============================================================
    // 17. UPDATE ADMIN PROFILE
    // ============================================================
    public async Task<bool> UpdateAdminProfileAsync(int userId, UpdateAdminProfileDto updateAdminProfileDto)
    {
        // CHECK 1: User exists?
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException(
                "User not found.");

        // CHECK 2: Employee record exists?
        var employee = await _employeeRepository.GetByUserIdAsync(userId);
        if (employee == null)
            throw new NotFoundException(
                "Employee profile not found. Please contact support.");

        var emailChanged = !string.Equals(user.Email, updateAdminProfileDto.Email, StringComparison.OrdinalIgnoreCase);

        // CHECK 3: New email already taken?
        if (emailChanged)
        {
            var existing = await _userRepository.GetByEmailAsync(updateAdminProfileDto.Email);
            if (existing != null && existing.Id != userId)
                throw new ConflictException(
                    "This email is already registered with another account. Please use a different email.");
        }

        // CHECK 4: DateOfBirth valid range? (adult, aur bahut purani date nahi)
        if (updateAdminProfileDto.DateOfBirth.HasValue)
        {
            var dob = updateAdminProfileDto.DateOfBirth.Value;
            var today = DateTime.UtcNow;
            var minAllowedYear = today.Year - 100;
            var maxAllowedYear = today.Year - 18;

            if (dob.Year < minAllowedYear)
                throw new BadRequestException($"Date of birth year must be {minAllowedYear} or later.");

            if (dob.Year > maxAllowedYear)
                throw new BadRequestException("You must be at least 18 years old.");
        }

        employee.FirstName = updateAdminProfileDto.FirstName.Trim();
        employee.LastName = string.IsNullOrWhiteSpace(updateAdminProfileDto.LastName) ? null : updateAdminProfileDto.LastName.Trim();
        employee.PhoneNumber = updateAdminProfileDto.PhoneNumber.Trim();
        employee.PresentAddress = updateAdminProfileDto.PresentAddress;
        employee.DateOfBirth = updateAdminProfileDto.DateOfBirth;

        if (emailChanged)
            employee.Email = updateAdminProfileDto.Email.Trim();

        await _employeeRepository.UpdateAsync(employee);

        if (emailChanged)
        {
            user.Email = updateAdminProfileDto.Email.Trim();
            await _userRepository.UpdateAsync(user);
            await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id);
        }

        return emailChanged;
    }

    // ============================================================
    // 18. ADMIN CHANGE PASSWORD
    // ============================================================
    public async Task ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        // CHECK 1: User exists?
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new NotFoundException(
                "User not found.");

        // CHECK 2: Current password correct?
        if (!VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHash))
            throw new BadRequestException(
                "Your current password is incorrect. Please try again.");

        // CHECK 3: New password different?
        if (changePasswordDto.CurrentPassword == changePasswordDto.NewPassword)
            throw new BadRequestException(
                "New password must be different from your current password.");

        // CHECK 4: New password matches confirm?
        if (changePasswordDto.NewPassword != changePasswordDto.ConfirmNewPassword)
            throw new BadRequestException(
                "New password and confirm password do not match.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        await _userRepository.UpdateAsync(user);
        await _refreshTokenRepository.RevokeAllByUserIdAsync(user.Id, "Password changed");

        _logger.LogInformation("Password changed: UserId={UserId}, Email={Email}", user.Id, user.Email);

        try { await _emailService.SendPasswordChangedEmailAsync(user.Email); }
        catch { }
    }

    // ============================================================
    // PRIVATE HELPERS (JWT Generation, Password Hashing) — unchanged (with JTI _ longer claims)
    // ============================================================
    private string GenerateAccessToken(User user, int expiryMinutes)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User cannot be null.");

        if (string.IsNullOrEmpty(_jwtSettings.SecretKey))
            throw new InvalidOperationException("JWT SecretKey is missing from configuration.");

        var secretKey = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        var jti = Guid.NewGuid().ToString();

        // Admin ka Full Name frontend mein bhej rhe hai welcome k saath use karne k liye JWT ke andar
        var firstName = user.Employee?.FirstName?.Trim() ?? string.Empty;
        var lastName = user.Employee?.LastName?.Trim() ?? string.Empty;
        var fullName = $"{firstName} {lastName}".Trim();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
            new Claim("role", user.Role.ToString()),
            new Claim("uid", user.Id.ToString()),
            new Claim("firstName", firstName),
            new Claim("lastName", lastName),
            new Claim("name", fullName)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // Tuple-returning version
    private (string Raw, string Hash) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var raw = Convert.ToBase64String(randomBytes);
        return (raw, HashToken(raw));
    }

    // SHA256 hash
    private static string HashToken(string rawToken)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}