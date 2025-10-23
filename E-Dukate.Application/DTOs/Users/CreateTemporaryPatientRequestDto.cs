namespace E_Dukate.Application.DTOs.Users;

public class CreateTemporaryPatientRequestDto
{
    public required string WhatsAppNumber { get; set; }
    public required TemporaryPatientDto PatientData { get; set; }
}