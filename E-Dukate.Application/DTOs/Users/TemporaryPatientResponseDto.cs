namespace E_Dukate.Application.DTOs.Users;

public class TemporaryPatientResponseDto
{
    public Guid Id { get; set; }
    public string Names { get; set; } = string.Empty;
    public string LastNamePaternal { get; set; } = string.Empty;
    public string? LastNameMaternal { get; set; }
    public int IdentityCard { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool IsConfirmed { get; set; }
    public DateTime ExpiresAt { get; set; }
}