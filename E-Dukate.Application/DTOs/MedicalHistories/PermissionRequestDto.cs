namespace E_Dukate.Application.DTOs.MedicalHistories;

public class PermissionRequestDto
{
    public Guid MedicalHistoryId { get; set; }
    public Guid SpecialistId { get; set; }
    public bool CanEdit { get; set; }
}
