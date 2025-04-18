namespace E_Dukate.Application.DTOs.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public required string Names { get; set; }
    public required string LastNamePaternal { get; set; }
    public required string LastNameMaternal { get; set; }
    public required string MobileNumber { get; set; }
    public required string Role { get; set; }
}