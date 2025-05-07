namespace E_Dukate.Domain.Entities.Users;

public abstract class User : Primitives.Entity
{
    public string Names { get; set; } = string.Empty;
    public string LastNamePaternal { get; set; } = string.Empty;
    public string? LastNameMaternal { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public int IdentityCard { get; set; }
    public string? PhoneNumber { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
}