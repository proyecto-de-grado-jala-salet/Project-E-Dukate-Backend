using E_Dukate.Application.DTOs.Schedules;

namespace E_Dukate.Application.DTOs.Appointments;

public class AppointmentDto
{
    public Guid PatientId { get; set; }
    public Guid SpecialtyId { get; set; }
    public Guid SpecialistId { get; set; }
    public int SessionCount { get; set; }
    public List<ScheduledSessionDto> ScheduledSessions { get; set; } = new List<ScheduledSessionDto>();
}
