using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Primitives;
using E_Dukate.Domain.Entities.Appointments;

namespace E_Dukate.Domain.Entities.TreatmentPlans;

public class TreatmentPlan : Entity
{
    public Guid PatientId { get; set; }
    public required Patient Patient { get; set; }
    public Guid SpecialistId { get; set; }
    public required Specialist Specialist { get; set; }
    public Guid SpecialtyId { get; set; }
    public required Specialty Specialty { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    public decimal TotalCost { get; set; }
    public decimal PendingAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EstimatedEndDate { get; set; }
    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Active;
}