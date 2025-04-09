namespace E_Dukate.Application.DTOs.Users;

public class AdministratorDto : BaseUserDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}