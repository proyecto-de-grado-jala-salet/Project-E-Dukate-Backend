namespace E_Dukate.Domain.Entities;

public class Administrator : User
{
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string AccessCode { get; set; }
}