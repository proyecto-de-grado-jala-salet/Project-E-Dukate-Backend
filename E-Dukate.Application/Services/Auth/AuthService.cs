using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Application.DTOs.Auth;
using E_Dukate.Domain.Primitives;
using E_Dukate.Application.Validators.Auth;
using E_Dukate.Application.Interfaces.Auth;

namespace E_Dukate.Application.Services.Auth;

public class AuthService
{
    private readonly IGenericRepository<UserAuth> _userAuthRepository;
    private readonly UserAuthValidator _validator;
    private readonly IUserProfileService _profileService;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly LoginLogger _loginLogger;

    public AuthService(
        IGenericRepository<UserAuth> userAuthRepository,
        UserAuthValidator validator,
        IUserProfileService profileService,
        JwtTokenGenerator tokenGenerator,
        LoginLogger loginLogger)
    {
        _userAuthRepository = userAuthRepository;
        _validator = validator;
        _profileService = profileService;
        _tokenGenerator = tokenGenerator;
        _loginLogger = loginLogger;
    }

    public async Task<ValueResult<string>> LoginAsync(LoginDto loginDto)
    {
        var validationResult = _validator.Validate(loginDto);
        if (!validationResult.IsValid)
            return ValueResult<string>.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var userAuth = await FindUserByEmailAsync(loginDto.Email);
        if (userAuth == null)
            return await _loginLogger.LogFailedLoginAsync(null, null, "Usuario no encontrado.");

        var passwordResult = await VerifyPasswordAsync(userAuth, loginDto.Password);
        if (!passwordResult.IsSuccess)
            return passwordResult;

        var fullName = await _profileService.GetFullNameAsync(userAuth.UserId, userAuth.UserRole);

        var token = _tokenGenerator.GenerateToken(userAuth.UserId, userAuth.UserRole, fullName);
        
        await _loginLogger.LogSuccessfulLoginAsync(userAuth.UserId, userAuth.UserRole);

        return ValueResult<string>.Success(token);
    }

    private async Task<UserAuth?> FindUserByEmailAsync(string email)
    {
        return _userAuthRepository.GetAll().FirstOrDefault(u => u.Email == email);
    }

    private async Task<ValueResult<string>> VerifyPasswordAsync(UserAuth userAuth, string password)
    {
        if (!BCrypt.Net.BCrypt.Verify(password, userAuth.PasswordHash))
        {
            return await _loginLogger.LogFailedLoginAsync(userAuth.UserId, userAuth.UserRole, "Contraseña incorrecta.");
        }
        
        return ValueResult<string>.Success("");
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
