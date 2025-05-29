using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Domain.Entities.Schedules;

namespace E_Dukate.Domain.Entities.Appointments;

public class Appointment : Primitives.Entity
{
    public Guid PatientId { get; set; }
    public required Patient Patient { get; set; }
    public Guid SpecialistId { get; set; }
    public required Specialist Specialist { get; set; }
    public Guid SpecialtyId { get; set; }
    public required Specialty Specialty { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<Schedule>? Schedules { get; set; } = new List<Schedule>();
    public AppointmentStatus? Status { get; set; }
    public int SessionCount { get; set; }
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
