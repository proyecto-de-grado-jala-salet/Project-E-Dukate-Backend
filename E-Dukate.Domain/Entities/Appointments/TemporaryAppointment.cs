using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.Appointments;

public class TemporaryAppointment : Primitives.Entity
{
    public string WhatsAppNumber { get; set; } = string.Empty;
    public string AppointmentData { get; set; } = string.Empty;
    public string? ComprobanteUrl { get; set; }
    public string? ComprobanteFileName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
    public bool IsProcessed { get; set; } = false;
    public string Status { get; set; } = "Pending";
    public DateTime? PaymentUploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
