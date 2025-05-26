namespace E_Dukate.Application.DTOs.MedicalHistories;

public class MedicalConsultationDto
{
    public Guid Id { get; set; }
    public Guid SpecialistId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ConsultationDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
