using E_Dukate.Application.DTOs.Metrics;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services.Metrics;

public class DemographicMetricsService
{
    private readonly IGenericRepository<Patient> _patientRepository;

    public DemographicMetricsService(IGenericRepository<Patient> patientRepository)
    {
        _patientRepository = patientRepository;
    }

    public async Task<DemographicMetricsDto> GetDemographicMetricsAsync(DemographicFilterDto filter)
    {
        var query = _patientRepository.GetAll()
            .AsNoTracking();
        
        if (filter.Genders != null && filter.Genders.Any())
        {
            query = query.Where(p => filter.Genders.Contains(p.Gender));
        }

        if (filter.MinAge.HasValue)
        {
            query = query.Where(p => p.Age >= filter.MinAge.Value);
        }

        if (filter.MaxAge.HasValue)
        {
            query = query.Where(p => p.Age <= filter.MaxAge.Value);
        }

        var patients = await query.ToListAsync();
        var totalPatients = patients.Count;
        
        var genderMetrics = patients
            .GroupBy(p => p.Gender)
            .Select(g => new GenderMetricDto
            {
                Gender = g.Key,
                Count = g.Count(),
                Percentage = totalPatients > 0 ? (g.Count() * 100.0 / totalPatients) : 0
            })
            .ToList();
        
        var ageMetrics = new List<AgeDistributionMetricDto>
        {
            new AgeDistributionMetricDto { AgeRange = "0-18", Count = patients.Count(p => p.Age <= 18) },
            new AgeDistributionMetricDto { AgeRange = "19-30", Count = patients.Count(p => p.Age >= 19 && p.Age <= 30) },
            new AgeDistributionMetricDto { AgeRange = "31-45", Count = patients.Count(p => p.Age >= 31 && p.Age <= 45) },
            new AgeDistributionMetricDto { AgeRange = "46-60", Count = patients.Count(p => p.Age >= 46 && p.Age <= 60) },
            new AgeDistributionMetricDto { AgeRange = "61+", Count = patients.Count(p => p.Age >= 61) }
        };

        return new DemographicMetricsDto
        {
            GenderMetrics = genderMetrics,
            AgeMetrics = ageMetrics,
            TotalPatients = totalPatients
        };
    }
}
