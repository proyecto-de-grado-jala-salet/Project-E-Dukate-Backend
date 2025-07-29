namespace E_Dukate.Application.DTOs.MedicalHistories;

public class MedicalDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}