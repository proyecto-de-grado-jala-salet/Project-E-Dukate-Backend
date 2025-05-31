using E_Dukate.Application.DTOs.Schedules;

namespace E_Dukate.Application.DTOs.Appointments;

public class AppointmentDto
{
    public Guid PatientId { get; set; }
    public Guid SpecialistId { get; set; }
    public Guid SpecialtyId { get; set; }
    public DateTime? StartTime { get; set; } // Hacer nullable
    public DateTime? EndTime { get; set; }   // Hacer nullable
    public int SessionCount { get; set; }
    public decimal SessionCost { get; set; }
    public List<ScheduleDto> Schedules { get; set; } = new List<ScheduleDto>();
}
