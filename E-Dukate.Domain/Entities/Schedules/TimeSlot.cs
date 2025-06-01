using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.Schedules;

public class TimeSlot : Entity
{
    public Guid ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}