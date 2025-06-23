namespace E_Dukate.Application.DTOs.Metrics;

public class MedicalHistoryMetricsDto
{
    public List<MedicalHistoryStatusMetricDto> Metrics { get; set; } = new List<MedicalHistoryStatusMetricDto>();
    public int Total { get; set; }
}