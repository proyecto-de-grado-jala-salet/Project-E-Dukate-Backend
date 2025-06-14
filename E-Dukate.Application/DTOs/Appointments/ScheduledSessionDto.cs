namespace E_Dukate.Application.DTOs.Appointments;

public class ScheduledSessionDto
{
    public Guid TimeSlotId { get; set; }
    public DateTime SessionDateTime { get; set; }
    public string Status { get; set; } = "Scheduled";
}
