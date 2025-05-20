using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.MedicalHistories;

public class MedicalConsultation : Entity
{
    public Guid PermissionId { get; set; }
    public MedicalHistoryPermission? Permission { get; set; }
    public Guid SpecialistId { get; set; }
    public Specialist? Specialist { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ConsultationDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
