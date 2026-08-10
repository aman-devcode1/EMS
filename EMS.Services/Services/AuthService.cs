using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using EMS.Core.DTOs.Auth;
using EMS.Core.DTOs.Admin;
using EMS.Core.DTOs.Employee;
using EMS.Core.Entities;
using EMS.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using EMS.Core.Enums;
using EMS.Services.Mapping;
using Azure.Core;

namespace EMS.Services.Services;

public class AuthService : IAuthService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AuthService(IRefreshTokenRepository refreshTokenRepository, IEmployeeRepository employeeRepository, IUserRepository userRepository, IConfiguration configuration, IMapper mapper)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _configuration = configuration;
        _mapper = mapper;
    }


    // ============================================================
    // 1. REGISTER (Naya User + Employee)
    // ============================================================
    public async Task<TokenResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // 🔥 Check: क्या Email पहले से है?
        var existingUser = await _userRepository.GetByEmailAsync(registerDto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email is already registered.");

        // 🔥 User Create करें (AutoMapper से)
        var user = _mapper.Map<User>(registerDto);

        // PasswordHash को Manual Set करें (क्योंकि AutoMapper नहीं कर सकता)
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password); // Initialize with hashed password
        user.Role = RoleType.Employee;
        user.IsActive = true;
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.AddAsync(user);

        // Employee Create करें (AutoMapper से)
        var employee = _mapper.Map<Employee>(registerDto);

        // UserId और System Fields को Manual Set करें
        employee.UserId = user.Id;
        employee.IsActive = true;
        employee.CreatedAt = DateTime.UtcNow;
        // Department, Designation, Salary, HireDate -> NULL (Admin बाद में भरेगा)

        // Optional Fields (अगर DTO में NULL आया तो Empty String)
        employee.PresentAddress ??= string.Empty;
        employee.PreviousCompanyRole ??= string.Empty;

        await _employeeRepository.AddAsync(employee);


        // 🔥 Tokens Generate करो
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // 🔥 Refresh Token Database में Save करो
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 दिन के लिए valid
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900, // 15 minutes in seconds
            TokenType = "Bearer"
        };
    }


    // ============================================================
    // 1. LOGIN (Access + Refresh Token Generate)
    // ============================================================
    public async Task<TokenResponseDto> LoginAsync(LoginDto loginDto)
    {
        // Step 1 :  User ढूँढो
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);
        if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Step 2: Tokens Generate करो
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // Step 3: Refresh Token Database में Save करो
        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 दिन के लिए valid
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900, // 15 minutes in seconds
            TokenType = "Bearer"
        };
    }

    // ============================================================
    // 2. REFRESH TOKEN (पुराने Token से नया Access Token लेना)
    // ============================================================
    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        // Step 1: Database से Token ढूँढो (User के साथ)
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (storedToken == null)
            throw new UnauthorizedAccessException("Refresh token not found.");

        // Step 2: Validate करो (Expired? Revoked?)
        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token hass expired. Please login again.");

        if (storedToken.IsRevoked)
            throw new UnauthorizedAccessException("Refresh token has been revoked. Please login again.");

        // Step 3: नया Access Token Generate करो
        var user = storedToken.User ?? throw new UnauthorizedAccessException("User not found for the provided refresh token.");
        var newAccessToken = GenerateAccessToken(user);

        // Step 4: (Optional) Refresh Token को Rotate करो - पुराने को हटाकर नया Refresh Token दो
        // यहाँ हम पुराने को Revoke करके नया Generate करेंगे (अतिरिक्त सुरक्षा)
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.UpdateAsync(storedToken);

        // नया Refresh Token Generate करो
        var newRefreshToken = GenerateRefreshToken();
        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity);

        return new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 900, // 15 minutes in seconds
            TokenType = "Bearer"
        };
    }

    // ============================================================
    // 3. REVOKE (Logout)
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
    // 4. REVOKE ALL TOKENS (Logout All Devices)
    // ============================================================
    public async Task<bool> RevokeAllTokensAsync(int userId)
    {
        await _refreshTokenRepository.RevokeAllByUserIdAsync(userId);
        return true;
    }

    // ============================================================
    // 5. Admin Registration (सिर्फ एक बार)
    // ============================================================
    public async Task<TokenResponseDto> AdminRegisterAsync(AdminRegisterDto adminRegisterDto)
    {
        // 🔥 Security Check: क्या पहले से कोई Admin है?
        var existingAdmin = await _userRepository.GetAdminAsync();
        if (existingAdmin != null)
            throw new InvalidOperationException("An admin already exists. Only one admin is allowed.");

        // 🔥 Check: क्या Email पहले से है?
        var existingUser = await _userRepository.GetByEmailAsync(adminRegisterDto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email is already Registered.");

        // 🔥 🔥 Bug Fix: Password और ConfirmPassword Match करो!
        if (adminRegisterDto.Password != adminRegisterDto.ConfirmPassword)
        throw new InvalidOperationException("Password and Confirmed Password does not match.");

        // 🔥 Step 1: User Create (Role = Admin)
        var user = _mapper.Map<User>(adminRegisterDto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminRegisterDto.Password);

        user.Role = RoleType.Admin;
        user.IsActive = true;
        user.CreatedAt = DateTime.UtcNow;

        await _userRepository.AddAsync(user);

        // 🔥 Step 2: Employee Create (Admin की Profile)
        var employee = _mapper.Map<Employee>(adminRegisterDto);
        employee.UserId = user.Id;
        employee.Designation = "System Administrator";
        employee.IsActive = true;
        employee.CreatedAt = DateTime.UtcNow;

        await _employeeRepository.AddAsync(employee);

        // 🔥 Step 3: Tokens Generate
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            TokenType = "Bearer"
        };
    }

// ============================================================
    // 6. Admin Login
    // ============================================================
    public async Task<TokenResponseDto> AdminLoginAsync(AdminLoginDto adminLoginDto)
    {
        // Step 1: User ढूँढो
        var user = await _userRepository.GetByEmailAsync(adminLoginDto.Email);
        if (user == null || user.Role != RoleType.Admin)
        throw new UnauthorizedAccessException("Invalid Admin Credentials.");

        if (!VerifyPassword(adminLoginDto.Password, user.PasswordHash))
        throw new UnauthorizedAccessException("Invalid Admin Credentials.");

        // Step 2: Tokens Generate
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        // Step 3: Refresh Token Database में Save
        var refreshTokenEnttiy = new RefreshToken
        {
          UserId = user.Id,
          Token = refreshToken,
          ExpiresAt = DateTime.UtcNow.AddDays(7),
          IsActive = false,
          CreatedAt = DateTime.UtcNow  
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEnttiy);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 900,
            TokenType = "Bearer"
        };
    }

    // ============================================================
    // PRIVATE HELPERS (JWT Generation, Password Hashing)
    // ============================================================
    private string GenerateAccessToken(User user)
    {
        // 🔥 Null Check 1: User
        if (user == null)
            throw new ArgumentNullException(nameof(user), "User cannot be null.");

        var jwtSettings = _configuration.GetSection("JwtSettings");

        // 🔥 Null Check 2: SecretKey
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
            Expires = DateTime.UtcNow.AddMinutes(15), // 15 minutes validity
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
        return Convert.ToBase64String(randomBytes);  // Random Unique String        
    }

    // 🔥 CORRECTED PRODUCTION-READY VerifyPassword
    private bool VerifyPassword(string password, string hash)
    {
        // BCrypt.Verify internally salt ko hash se nikal kar compare karta hai
        // Agar hash null/empty hai toh false return karo
        if (string.IsNullOrEmpty(hash))
            return false;

        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}