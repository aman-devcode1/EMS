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
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EMS.Services.Services;

public class AuthService : IAuthService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOtpRepository _otpRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AuthService(
        IRefreshTokenRepository refreshTokenRepository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IOtpRepository otpRepository,
        IEmailService emailService,
        IConfiguration configuration,
        IMapper mapper)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _emailService = emailService;
        _configuration = configuration;
        _mapper = mapper;
    }

    // ============================================================
    // OTP HELPER — Fetch → Expiry check → Delete → Generate → Email
    // ============================================================
    private async Task<OtpSentResponseDto> GenerateAndSendOtpAsync(int userId, string email, OtpPurpose purpose)
    {
        var existingOtp = await _otpRepository.GetByUserIdAsync(userId);
        string? oldCodeToShow = null;

        if (existingOtp != null)
        {
            // Agar purana OTP abhi bhi valid hai (5 min khatam nahi hua) — resend flow
            if (existingOtp.ExpiresAt > DateTime.UtcNow)
            {
                oldCodeToShow = existingOtp.Code;
            }
            // Chahe valid ho ya expired, purana record hatao (naya banayenge)
            await _otpRepository.DeleteAsync(existingOtp.Id);
        }

        var newCode = GenerateOtpCode();

        var otpEntity = new OtpCode
        {
            UserId = userId,
            Code = newCode,
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        };

        await _otpRepository.AddAsync(otpEntity);
        await _emailService.SendOtpEmailAsync(email, newCode, oldCodeToShow);

        return new OtpSentResponseDto
        {
            Message = "OTP has been sent to your registered email address.",
            Email = email
        };
    }

    private string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    // ============================================================
    // COMMON: Token Issuance — sabke liye refresh token 1 din
    // ============================================================
    private async Task<TokenResponseDto> IssueTokensAsync(User user)
    {
        var accessToken = GenerateAccessToken(user);
        var newRefreshTokenValue = GenerateRefreshToken();

        // 🔥 Unique-per-user constraint fix: purana token UPDATE karo, naya INSERT mat karo
        var existingRefreshToken = await _refreshTokenRepository.GetByUserIdAsync(user.Id);

        if (existingRefreshToken != null)
        {
            existingRefreshToken.Token = newRefreshTokenValue;
            existingRefreshToken.ExpiresAt = DateTime.UtcNow.AddDays(1);
            existingRefreshToken.IsRevoked = false;
            existingRefreshToken.RevokedAt = null;
            await _refreshTokenRepository.UpdateAsync(existingRefreshToken);
        }
        else
        {
            await _refreshTokenRepository.CreateAsync(new RefreshToken
            {
                UserId = user.Id,
                Token = newRefreshTokenValue,
                ExpiresAt = DateTime.UtcNow.AddDays(1),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshTokenValue,
            ExpiresIn = 900,
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
            throw new ConflictException("Email is already registered.");

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

        return await GenerateAndSendOtpAsync(user.Id, user.Email, OtpPurpose.Registration);
    }

    // ============================================================
    // 2. LOGIN (Employee) — Direct, OTP nahi
    // ============================================================
    public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email, phone number or password.");

        if (user.Employee == null || user.Employee.PhoneNumber != loginDto.PhoneNumber)
            throw new UnauthorizedAccessException("Invalid email, phone number or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is not verified yet. Please complete OTP verification.");

        return await IssueTokensAsync(user);
    }

    // ============================================================
    // 3. REFRESH TOKEN
    // ============================================================
    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (storedToken == null)
            throw new UnauthorizedAccessException("Refresh token not found.");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired. Please login again.");

        if (storedToken.IsRevoked)
            throw new UnauthorizedAccessException("Refresh token has been revoked. Please login again.");

        var user = storedToken.User ?? throw new UnauthorizedAccessException("User not found for the provided refresh token.");

        return await IssueTokensAsync(user);
    }

    // ============================================================
    // 4. REVOKE (Logout)
    // ============================================================
    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (storedToken == null)
            return false;

        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken);
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
        var existingAdmin = await _userRepository.GetAdminAsync();
        if (existingAdmin != null)
            throw new ConflictException("An admin already exists. Only one admin is allowed.");

        var existingUser = await _userRepository.GetByEmailAsync(adminRegisterDto.Email);
        if (existingUser != null)
            throw new ConflictException("Email is already registered.");

        if (adminRegisterDto.Password != adminRegisterDto.ConfirmPassword)
            throw new BadRequestException("Password and Confirm Password do not match.");

        var user = _mapper.Map<User>(adminRegisterDto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminRegisterDto.Password);
        user.Role = RoleType.Admin;
        user.IsActive = false;
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.AddAsync(user);

        var employee = _mapper.Map<Employee>(adminRegisterDto);
        employee.UserId = user.Id;
        employee.Designation = "System Administrator";
        employee.IsActive = false;
        employee.CreatedAt = DateTime.UtcNow;

        await _employeeRepository.AddAsync(employee);

        return await GenerateAndSendOtpAsync(user.Id, user.Email, OtpPurpose.Registration);
    }

    // ============================================================
    // 7. ADMIN / MANAGER LOGIN — Step 1: Password+Phone check → OTP bhejo
    // ============================================================
    public async Task<OtpSentResponseDto> AdminLoginAsync(AdminLoginDto adminLoginDto)
    {
        var user = await _userRepository.GetByEmailAsync(adminLoginDto.Email);
        if (user == null || (user.Role != RoleType.Admin && user.Role != RoleType.Manager))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!VerifyPassword(adminLoginDto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (user.Employee == null || user.Employee.PhoneNumber != adminLoginDto.PhoneNumber)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is not verified yet.");

        return await GenerateAndSendOtpAsync(user.Id, user.Email, OtpPurpose.Login);
    }

    // ============================================================
    // 8. VERIFY REGISTRATION OTP (Employee + Admin dono)
    // ============================================================
    public async Task<TokenResponseDto> VerifyRegistrationOtpAsync(VerifyOtpDto verifyOtpDto)
    {
        var user = await _userRepository.GetByEmailAsync(verifyOtpDto.Email)
            ?? throw new NotFoundException("User not found.");

        var otp = await _otpRepository.GetByUserIdAsync(user.Id);
        if (otp == null || otp.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("OTP has expired. Please request a new one.");

        if (otp.Code != verifyOtpDto.Code)
            throw new BadRequestException("Invalid OTP code.");

        await _otpRepository.DeleteAsync(otp.Id);

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
        var user = await _userRepository.GetByEmailAsync(verifyOtpDto.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var otp = await _otpRepository.GetByUserIdAsync(user.Id);
        if (otp == null || otp.ExpiresAt < DateTime.UtcNow)
            throw new BadRequestException("OTP has expired. Please login again.");

        if (otp.Code != verifyOtpDto.Code)
            throw new BadRequestException("Invalid OTP code.");

        await _otpRepository.DeleteAsync(otp.Id);

        return await IssueTokensAsync(user);
    }

    // ============================================================
    // PRIVATE HELPERS (JWT Generation, Password Hashing) — unchanged
    // ============================================================
    private string GenerateAccessToken(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User cannot be null.");

        var jwtSettings = _configuration.GetSection("JwtSettings");

        var secretKeyString = jwtSettings["SecretKey"];
        if (string.IsNullOrEmpty(secretKeyString))
            throw new InvalidOperationException("JWT SecretKey is missing from configuration.");

        var secretKey = Encoding.UTF8.GetBytes(secretKeyString);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("uid", user.Id.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}