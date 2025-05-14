namespace E_Dukate.Application.DTOs.MedicalHistories;

public class MedicalHistoryDto
{
    public Guid PatientId { get; set; }
    public List<MedicalHistoryPermissionDto> MedicalHistoryPermissions { get; set; } = new List<MedicalHistoryPermissionDto>();
}
