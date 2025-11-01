using System;

namespace E_Dukate.Application.DTOs.Appointments;

public class ReschedulePreviewRequestDto
{
    public Guid SessionId { get; set; }
    public string TargetDayOfWeek { get; set; } = string.Empty; // "Monday", "Tuesday", etc.
    public DateTime? SpecificDate { get; set; } // Fecha específica si se quiere
    public int LookAheadWeeks { get; set; } = 2; // Número de semanas a buscar
}
