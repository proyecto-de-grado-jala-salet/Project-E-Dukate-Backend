using E_Dukate.Domain.Entities.MedicalHistories;

namespace E_Dukate.Application.DTOs.MedicalHistories;

public class UpdateMedicalHistoryStatusDto
{
    public MedicalHistoryStatus Status { get; set; }
}
