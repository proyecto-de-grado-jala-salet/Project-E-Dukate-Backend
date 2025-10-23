using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.Users;

public class TemporaryPatient : Entity
{
    public string WhatsAppNumber { get; set; } = string.Empty;
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
    public bool IsConfirmed { get; set; } = false;
    public Guid? RealPatientId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}