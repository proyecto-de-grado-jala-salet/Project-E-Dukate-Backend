namespace E_Dukate.Application.DTOs.Appointments;

public class AppointmentDto
{
    public required Guid PatientId { get; set; }
    public required Guid SpecialistId { get; set; }
    public required Guid SpecialtyId { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public int SessionCount { get; set; }
    public decimal SessionCost { get; set; }
}
