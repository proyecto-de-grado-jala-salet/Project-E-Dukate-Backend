using E_Dukate.Domain.Entities.MedicalHistories;

namespace E_Dukate.Application.DTOs.MedicalHistories;

public class MedicalHistoryPermissionDto
{
    public Guid SpecialistId { get; set; }
    public bool CanEdit { get; set; }
    public MedicalHistoryStatus Status { get; set; }
    public List<MedicalConsultationDto> MedicalConsultations { get; set; } = new List<MedicalConsultationDto>();
}