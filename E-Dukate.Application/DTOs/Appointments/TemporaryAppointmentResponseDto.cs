using System;

namespace E_Dukate.Application.DTOs.Appointments;

public class TemporaryAppointmentResponseDto
{
    public Guid Id { get; set; }
    public string WhatsAppNumber { get; set; } = string.Empty;
    public object AppointmentData { get; set; } = new();
    public string? ComprobanteUrl { get; set; }
    public string? ComprobanteFileName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsProcessed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaymentUploadedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
