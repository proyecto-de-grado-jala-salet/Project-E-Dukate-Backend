namespace E_Dukate.Application.DTOs.MedicalHistories;

public class UpdateMedicalConsultationDto
{
    public string Reason { get; set; } = string.Empty;
    public DateTime ConsultationDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
