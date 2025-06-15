namespace E_Dukate.Application.DTOs.Appointments;

public class ScheduledSessionDto
{
    public Guid TimeSlotId { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Status { get; set; } = "Scheduled";
}
