using System;

namespace E_Dukate.Application.DTOs.Appointments;

public class AvailableTimeSlotDto
{
    public Guid TimeSlotId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public string FormattedDate { get; set; } = string.Empty;
    public string FormattedTime { get; set; } = string.Empty;
    public bool IsSameDay { get; set; }
    public bool IsNextWeek { get; set; }
}
