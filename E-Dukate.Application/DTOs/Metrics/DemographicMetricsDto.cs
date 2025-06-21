namespace E_Dukate.Application.DTOs.Metrics;

public class DemographicMetricsDto
{
    public List<GenderMetricDto> GenderMetrics { get; set; } = new List<GenderMetricDto>();
    public List<AgeDistributionMetricDto> AgeMetrics { get; set; } = new List<AgeDistributionMetricDto>();
    public int TotalPatients { get; set; }
}
