using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Application.DTOs.Auth;
using FluentValidation;
using E_Dukate.Domain.Primitives;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Application.Services.Auth;

public class AuthService
{
    private readonly IGenericRepository<UserAuth> _userAuthRepository;
    private readonly IGenericRepository<LoginLog> _loginLogRepository;
    private readonly IGenericRepository<Administrator> _administratorRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IValidator<LoginDto> _validator;
    private readonly IConfiguration _configuration;

    public AuthService(
        IGenericRepository<UserAuth> userAuthRepository,
        IGenericRepository<LoginLog> loginLogRepository,
        IGenericRepository<Administrator> administratorRepository,
        IGenericRepository<Specialist> specialistRepository,
        IGenericRepository<Patient> patientRepository,
        IValidator<LoginDto> validator,
        IConfiguration configuration)
    {
        _userAuthRepository = userAuthRepository;
        _loginLogRepository = loginLogRepository;
        _administratorRepository = administratorRepository;
        _specialistRepository = specialistRepository;
        _patientRepository = patientRepository;
        _validator = validator;
        _configuration = configuration;
    }

    public async Task<ValueResult<string>> LoginAsync(LoginDto loginDto)
    {
        var validationResult = _validator.Validate(loginDto);
        if (!validationResult.IsValid)
            return ValueResult<string>.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var userAuth = _userAuthRepository.GetAll().FirstOrDefault(u => u.Email == loginDto.Email);

        if (userAuth == null)
            return await LogFailedLogin(null, null, "Usuario no encontrado.");

        if (userAuth.LockoutEnd.HasValue && userAuth.LockoutEnd > DateTime.UtcNow)
            return await LogFailedLogin(userAuth.UserId, userAuth.UserRole, $"Cuenta bloqueada hasta {userAuth.LockoutEnd.Value}.");

        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, userAuth.PasswordHash))
        {
            userAuth.FailedLoginAttempts++;
            if (userAuth.FailedLoginAttempts >= 3)
            {
                userAuth.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                userAuth.FailedLoginAttempts = 0;
            }
            await _userAuthRepository.UpdateAsync(userAuth);
            return await LogFailedLogin(userAuth.UserId, userAuth.UserRole, "Contraseña incorrecta.");
        }

        if (userAuth.FailedLoginAttempts > 0 || userAuth.LockoutEnd.HasValue)
        {
            userAuth.FailedLoginAttempts = 0;
            userAuth.LockoutEnd = null;
            await _userAuthRepository.UpdateAsync(userAuth);
        }
        
        string fullName = "Usuario Desconocido";
        switch (userAuth.UserRole.ToLower())
        {
            case "administrator":
                var admin = _administratorRepository.GetAll().FirstOrDefault(a => a.Id == userAuth.UserId);
                if (admin != null)
                    fullName = $"{admin.Names} {admin.LastNamePaternal}";
                break;
            case "specialist":
                var specialist = _specialistRepository.GetAll().FirstOrDefault(s => s.Id == userAuth.UserId);
                if (specialist != null)
                    fullName = $"{specialist.Names} {specialist.LastNamePaternal}";
                break;
            case "patient":
                var patient = _patientRepository.GetAll().FirstOrDefault(p => p.Id == userAuth.UserId);
                if (patient != null)
                    fullName = $"{patient.Names} {patient.LastNamePaternal}";
                break;
        }

        var token = GenerateJwtToken(userAuth.UserId, userAuth.UserRole, fullName);

        await LogSuccessfulLogin(userAuth.UserId, userAuth.UserRole);

        return ValueResult<string>.Success(token);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private string GenerateJwtToken(Guid userId, string role, string name)
    {
        var keyValue = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(keyValue))
            throw new InvalidOperationException("JWT Key is not configured.");

        var expirationValue = _configuration["Jwt:TokenExpirationInMinutes"];
        if (!int.TryParse(expirationValue, out var expirationMinutes))
            throw new InvalidOperationException("JWT TokenExpirationInMinutes is not configured or invalid.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Name, name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(expirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<ValueResult<string>> LogFailedLogin(Guid? userId, string? role, string? errorMessage)
    {
        var loginLog = new LoginLog
        {
            UserId = userId ?? Guid.Empty,
            UserRole = role ?? "Unknown",
            LoginTime = DateTime.UtcNow,
            IsSuccessful = false
        };
        await _loginLogRepository.AddAsync(loginLog);
        return ValueResult<string>.Failure(errorMessage ?? string.Empty);
    }

    private async Task LogSuccessfulLogin(Guid userId, string role)
    {
        var loginLog = new LoginLog
        {
            UserId = userId,
            UserRole = role,
            LoginTime = DateTime.UtcNow,
            IsSuccessful = true
        };
        await _loginLogRepository.AddAsync(loginLog);
    }
}