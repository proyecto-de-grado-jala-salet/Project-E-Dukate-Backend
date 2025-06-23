namespace E_Dukate.Application.DTOs.Metrics;

public class MedicalHistoryFilterDto
{
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    public List<string>? Statuses { get; set; }
}