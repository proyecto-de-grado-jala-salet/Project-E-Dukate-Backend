namespace E_Dukate.Application.DTOs.MedicalHistories;

public class MedicalHistoryPermissionDto
{
    public Guid Id { get; set; }
    public Guid SpecialistId { get; set; }
    public bool CanEdit { get; set; }
    public string Status { get; set; } = "ContinuaEnTratamiento";
    public List<MedicalConsultationDto> Consultations { get; set; } = new List<MedicalConsultationDto>();
}
