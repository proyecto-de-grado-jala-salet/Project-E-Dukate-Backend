namespace E_Dukate.Domain.Entities.Auth;

public class UserAuth : Primitives.Entity
{
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }
}