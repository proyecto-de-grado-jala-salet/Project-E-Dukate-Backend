using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Domain.Entities.Appointments;

public class Appointment
{
    public Guid PatientId { get; set; }
    public required Patient Patient { get; set; }
    public Guid SpecialistId { get; set; }
    public required Specialist Specialist { get; set; }
    public Guid SpecialtyId { get; set; }
    public required Specialty Specialty { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Scheduled";
}
