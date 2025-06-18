using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.DTOs.Schedules;

public class TimeSlotDto : Entity
{
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}