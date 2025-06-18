using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.Appointments;

public class ScheduledSession : Entity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; } = null!;
    public Guid TimeSlotId { get; set; }
    public TimeSlot? TimeSlot { get; set; } = null!;
    public DateTime StartSessionDateTime { get; set; }
    public DateTime EndSessionDateTime { get; set; }
    public ScheduledSessionStatus Status { get; set; } = ScheduledSessionStatus.Scheduled;
}
