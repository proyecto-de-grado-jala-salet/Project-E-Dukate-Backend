using E_Dukate.Domain.Entities.MedicalHistories;

namespace E_Dukate.Domain.Entities.Users;

public class Patient : User
{
    public Guid MedicalHistoryId { get; set; }
    public MedicalHistory? MedicalHistory { get; set; }
}