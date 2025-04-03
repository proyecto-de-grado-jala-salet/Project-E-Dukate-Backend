namespace E_Dukate.Domain.Entities;

public abstract class User : Primitives.Entity
{
    public required string Names { get; set; }
    public required string LastNamePaternal { get; set; }
    public required string LastNameMaternal { get; set; }
    public required string MobileNumber { get; set; }
    public required int IdentityCard { get; set; }
    public string? PhoneNumber { get; set; }
    public required int Age { get; set; }
    public required string Gender { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string Address { get; set; }
}