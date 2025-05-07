namespace E_Dukate.Application.DTOs.Appointments;

public class AppointmentDto
{
    public Guid PatientId { get; set; }
    public Guid SpecialistId { get; set; }
    public Guid SpecialtyId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "Scheduled";
}
