namespace E_Dukate.Application.DTOs.MedicalHistories;

public class MedicalHistoryDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public List<MedicalHistoryPermissionDto> Permissions { get; set; } = new List<MedicalHistoryPermissionDto>();
}
