namespace E_Dukate.Application.DTOs.Metrics;

public class DemographicFilterDto
{
    public List<string>? Genders { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}
