using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Payments;

namespace E_Dukate.Domain.Entities.Appointments;

public class Appointment : Primitives.Entity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
    public Guid SpecialistId { get; set; }
    public Specialist Specialist { get; set; } = null!;
    public int SessionCount { get; set; }
    public List<ScheduledSession> ScheduledSessions { get; set; } = new List<ScheduledSession>();
    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
