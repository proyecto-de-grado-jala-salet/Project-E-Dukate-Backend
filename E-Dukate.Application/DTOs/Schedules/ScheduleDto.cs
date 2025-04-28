namespace E_Dukate.Application.DTOs.Schedules;

public class ScheduleDto
{
    public string? DayOfWeek { get; set; }
    public List<TimeSlotDto> TimeSlots { get; set; } = new List<TimeSlotDto>();
    public bool Attends { get; set; } = true;
}