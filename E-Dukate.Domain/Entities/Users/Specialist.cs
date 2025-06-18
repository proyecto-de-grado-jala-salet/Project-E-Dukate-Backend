using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Entities.Specialties;

namespace E_Dukate.Domain.Entities.Users;

public class Specialist : User
{
    public required Specialty Specialty { get; set; }
    public required int YearsOfExperience { get; set; }
    public required string SpecialistCode { get; set; }
    public int ConsultationDuration { get; set; }
    public List<Schedule> Schedules { get; set; } = new List<Schedule>();
    public List<Appointment>? Appointments { get; set; } = new List<Appointment>();
    public List<Payment>? Payments { get; set; } = new List<Payment>();
}