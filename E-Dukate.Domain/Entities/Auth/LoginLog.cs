namespace E_Dukate.Domain.Entities.Auth;

public class LoginLog : Primitives.Entity
{
    public Guid UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
    public bool IsSuccessful { get; set; }
}