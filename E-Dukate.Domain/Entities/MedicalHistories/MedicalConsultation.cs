using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.MedicalHistories;

public class MedicalConsultation : Entity
{
    public Guid MedicalHistoryId { get; set; }
    public required MedicalHistory MedicalHistory { get; set; }
    public Guid SpecialistId { get; set; }
    public required Specialist Specialist { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ConsultationDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
