using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Application.DTOs.Metrics;

namespace E_Dukate.Application.Services.Metrics;

public class MedicalHistoryMetricsService
{
    private readonly IGenericRepository<MedicalHistoryPermission> _permissionRepository;

    public MedicalHistoryMetricsService(IGenericRepository<MedicalHistoryPermission> permissionRepository)
    {
        _permissionRepository = permissionRepository;
    }

    public async Task<MedicalHistoryMetricsDto> GetMetricsAsync(MedicalHistoryFilterDto filter)
    {
        var query = _permissionRepository.GetAll()
            .Include(p => p.MedicalHistory)
            .Include(p => p.Consultations)
            .AsNoTracking();
        
        if (filter.Statuses != null && filter.Statuses.Any())
        {
            if (filter.Statuses.Any(s => !Enum.TryParse<MedicalHistoryStatus>(s, true, out _)))
            {
                throw new ArgumentException("One or more invalid status values provided.");
            }
            var statuses = filter.Statuses
                .Where(s => Enum.TryParse<MedicalHistoryStatus>(s, true, out var status))
                .Select(s => Enum.Parse<MedicalHistoryStatus>(s, true))
                .ToList();
            query = query.Where(p => statuses.Contains(p.Status));
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(p => p.Consultations.Any(c => c.ConsultationDate.Year == filter.Year.Value));
        }

        if (filter.Month.HasValue)
        {
            query = query.Where(p => p.Consultations.Any(c => c.ConsultationDate.Month == filter.Month.Value));
        }

        if (filter.Day.HasValue)
        {
            query = query.Where(p => p.Consultations.Any(c => c.ConsultationDate.Day == filter.Day.Value));
        }
        
        var metrics = await query
            .GroupBy(p => p.Status)
            .Select(g => new
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();
        
        var result = new MedicalHistoryMetricsDto
        {
            Metrics = metrics.Select(m => new MedicalHistoryStatusMetricDto
            {
                Status = m.Status,
                Count = m.Count
            }).ToList(),
            Total = metrics.Sum(m => m.Count)
        };

        return result;
    }
}