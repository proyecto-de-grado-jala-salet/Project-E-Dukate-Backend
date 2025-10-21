using System;

namespace E_Dukate.Application.DTOs.Appointments;

public class CreateTemporaryAppointmentRequestDto
{
    public string WhatsAppNumber { get; set; } = string.Empty;
    public object AppointmentData { get; set; } = new();
}
