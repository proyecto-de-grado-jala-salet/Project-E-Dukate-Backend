using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.MedicalHistories;

public class MedicalHistoryPermission : Entity
{
    public Guid MedicalHistoryId { get; set; }
    public MedicalHistory? MedicalHistory { get; set; }
    public Guid SpecialistId { get; set; }
    public Specialist? Specialist { get; set; }
    public bool CanEdit { get; set; } = true;
    public MedicalHistoryStatus Status { get; set; } = MedicalHistoryStatus.ContinuaEnTratamiento;
    public List<MedicalConsultation> Consultations { get; set; } = new List<MedicalConsultation>();
}