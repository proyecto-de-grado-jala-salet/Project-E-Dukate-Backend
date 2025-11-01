using System;

namespace E_Dukate.Application.DTOs.Appointments;

public class ReschedulePreviewRequestDto
{
    public Guid SessionId { get; set; }
    public string TargetDayOfWeek { get; set; } = string.Empty;
    public int LookAheadWeeks { get; set; } = 2;
}
