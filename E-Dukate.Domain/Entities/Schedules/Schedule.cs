using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Domain.Entities.Schedules;

public class Schedule : Primitives.Entity
{
    public Guid SpecialistId { get; set; }
    public required Specialist Specialist { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public List<TimeSlot> TimeSlots { get; set; } = new List<TimeSlot>();
    public bool Attends { get; set; } = true;
}