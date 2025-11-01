namespace E_Dukate.Application.DTOs.Appointments;

public class RescheduleSessionDto
{
    public Guid SessionId { get; set; }
    public Guid NewTimeSlotId { get; set; }
    public DateTime NewStartDateTime { get; set; }
    public DateTime NewEndDateTime { get; set; }
}