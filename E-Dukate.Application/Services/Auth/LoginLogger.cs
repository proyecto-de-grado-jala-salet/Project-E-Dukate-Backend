using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.Services.Auth;
public class LoginLogger
{
    private readonly IGenericRepository<LoginLog> _loginLogRepository;

    public LoginLogger(IGenericRepository<LoginLog> loginLogRepository)
    {
        _loginLogRepository = loginLogRepository;
    }

    public async Task<ValueResult<string>> LogFailedLoginAsync(Guid? userId, string? role, string errorMessage)
    {
        var loginLog = new LoginLog
        {
            UserId = userId ?? Guid.Empty,
            UserRole = role ?? "Unknown",
            LoginTime = DateTime.UtcNow,
            IsSuccessful = false
        };
        await _loginLogRepository.AddAsync(loginLog);
        return ValueResult<string>.Failure(errorMessage);
    }

    public async Task LogSuccessfulLoginAsync(Guid userId, string role)
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
