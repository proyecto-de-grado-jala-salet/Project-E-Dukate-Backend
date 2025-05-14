using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.MedicalHistories;

public class MedicalHistory : Entity
{
    public Guid PatientId { get; set; }
    public required Patient Patient { get; set; }
    public List<MedicalHistoryPermission> Permissions { get; set; } = new List<MedicalHistoryPermission>();
}
