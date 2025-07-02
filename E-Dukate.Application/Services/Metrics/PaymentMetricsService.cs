using E_Dukate.Application.Services.Payments;
using E_Dukate.Application.DTOs.Payments;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Application.DTOs.Metrics;

namespace E_Dukate.Application.Services.Metrics;

public class PaymentMetricsService
{
    private readonly PaymentService _paymentService;

    public PaymentMetricsService(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task<List<IncomeByPeriodDto>> GetTotalCompletedIncomeByPeriodAsync(string? periodType, DateTime? startDate, DateTime? endDate)
    {
        periodType = string.IsNullOrWhiteSpace(periodType) ? "Monthly" : periodType;

        if (!new[] { "monthly", "weekly", "yearly" }.Contains(periodType.ToLower()))
            throw new ArgumentException("El tipo de período debe ser 'Monthly', 'Weekly' o 'Yearly'.");

        var filter = new PaymentFilterDto
        {
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (payments, _) = await _paymentService.GetFilteredPaymentsAsync(filter);

        var validDates = payments
            .Where(p => p.LastPaymentDate.HasValue)
            .Select(p => p.LastPaymentDate!.Value)
            .ToList();

        DateTime defaultStartDate = startDate ?? (validDates.Any() ? validDates.Min() : DateTime.UtcNow.AddYears(-1));
        DateTime defaultEndDate = endDate ?? (validDates.Any() ? validDates.Max() : DateTime.UtcNow);

        var forPayments = payments
            .Where(p => p.LastPaymentDate.HasValue && p.LastPaymentDate.Value >= defaultStartDate && p.LastPaymentDate.Value <= defaultEndDate)
            .ToList();

        var result = new List<IncomeByPeriodDto>();

        switch (periodType.ToLower())
        {
            case "monthly":
                result = forPayments
                    .GroupBy(p => new
                    {
                        Year = p.LastPaymentDate!.Value.Year,
                        Month = p.LastPaymentDate!.Value.Month
                    })
                    .Select(g => new IncomeByPeriodDto
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:00}",
                        TotalIncome = g.Sum(p => p.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;

            case "weekly":
                result = forPayments
                    .GroupBy(p =>
                    {
                        var date = p.LastPaymentDate!.Value;
                        var daysFromMonday = (date.DayOfWeek - DayOfWeek.Monday + 7) % 7;
                        var weekStart = date.Date.AddDays(-daysFromMonday);
                        return new { Year = weekStart.Year, Month = weekStart.Month, Day = weekStart.Day };
                    })
                    .Select(g => new IncomeByPeriodDto
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:00}-{g.Key.Day:00} a {g.Key.Year}-{g.Key.Month:00}-{g.Key.Day + 6:00}",
                        TotalIncome = g.Sum(p => p.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;

            case "yearly":
                result = forPayments
                    .GroupBy(p => p.LastPaymentDate!.Value.Year)
                    .Select(g => new IncomeByPeriodDto
                    {
                        Period = g.Key.ToString(),
                        TotalIncome = g.Sum(p => p.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;
        }

        return result;
    }

    public async Task<PaymentStatusCountDto> GetPaymentStatusCountsAsync(DateTime? startDate, DateTime? endDate)
    {
        var filter = new PaymentFilterDto
        {
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (payments, _) = await _paymentService.GetFilteredPaymentsAsync(filter);

        var validDates = payments
            .Select(p => p.FirstPaymentDate.HasValue ? p.FirstPaymentDate.Value : p.LastPaymentDate.HasValue ? p.LastPaymentDate.Value : (DateTime?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        DateTime defaultStartDate = startDate ?? (validDates.Any() ? validDates.Min() : DateTime.UtcNow.AddYears(-1));
        DateTime defaultEndDate = endDate ?? (validDates.Any() ? validDates.Max() : DateTime.UtcNow);

        var filteredPayments = payments
            .Where(p => (p.FirstPaymentDate.HasValue && p.FirstPaymentDate.Value >= defaultStartDate && p.FirstPaymentDate.Value <= defaultEndDate) ||
                        (p.LastPaymentDate.HasValue && p.LastPaymentDate.Value >= defaultStartDate && p.LastPaymentDate.Value <= defaultEndDate))
            .ToList();

        return new PaymentStatusCountDto
        {
            PendingCount = filteredPayments.Count(p => p.Status == PaymentStatus.Pending),
            CompletedCount = filteredPayments.Count(p => p.Status == PaymentStatus.Completed)
        };
    }

    public async Task<InstitutionEarningsDto> GetInstitutionEarningsAsync(DateTime? startDate, DateTime? endDate)
    {
        var filter = new PaymentFilterDto
        {
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (payments, _) = await _paymentService.GetFilteredPaymentsAsync(filter);

        var validDates = payments
            .Select(p => p.FirstPaymentDate.HasValue ? p.FirstPaymentDate.Value : p.LastPaymentDate.HasValue ? p.LastPaymentDate.Value : (DateTime?)null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        DateTime defaultStartDate = startDate ?? (validDates.Any() ? validDates.Min() : DateTime.UtcNow.AddYears(-1));
        DateTime defaultEndDate = endDate ?? (validDates.Any() ? validDates.Max() : DateTime.UtcNow);

        var filteredPayments = payments
            .Where(p => (p.FirstPaymentDate.HasValue && p.FirstPaymentDate.Value >= defaultStartDate && p.FirstPaymentDate.Value <= defaultEndDate) ||
                        (p.LastPaymentDate.HasValue && p.LastPaymentDate.Value >= defaultStartDate && p.LastPaymentDate.Value <= defaultEndDate))
            .ToList();

        return new InstitutionEarningsDto
        {
            TotalInstitutionEarnings = filteredPayments.Sum(p => p.InstitutionAmount)
        };
    }

    public async Task<List<int>> GetAvailableYearsAsync()
    {
        var filter = new PaymentFilterDto
        {
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (payments, _) = await _paymentService.GetFilteredPaymentsAsync(filter);

        var years = payments
            .SelectMany(p => new[] { p.FirstPaymentDate?.Year, p.LastPaymentDate?.Year })
            .Where(y => y.HasValue)
            .Select(y => y!.Value)
            .Distinct()
            .OrderBy(y => y)
            .ToList();

        return years;
    }

    public async Task<PendingVsCompletedPaymentsDto> GetPendingVsCompletedPaymentsAsync(DateTime? startDate, DateTime? endDate)
    {
        var filter = new PaymentFilterDto
        {
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (payments, _) = await _paymentService.GetFilteredPaymentsAsync(filter);

        var filteredPayments = payments;
        if (startDate.HasValue && endDate.HasValue)
        {
            var validDates = payments
                .Select(p => p.FirstPaymentDate.HasValue ? p.FirstPaymentDate.Value : p.LastPaymentDate.HasValue ? p.LastPaymentDate.Value : (DateTime?)null)
                .Where(d => d.HasValue)
                .Select(d => d!.Value)
                .ToList();

            DateTime defaultStartDate = startDate.Value;
            DateTime defaultEndDate = endDate.Value;

            filteredPayments = payments
                .Where(p => (p.FirstPaymentDate.HasValue && p.FirstPaymentDate.Value >= defaultStartDate && p.FirstPaymentDate.Value <= defaultEndDate) ||
                            (p.LastPaymentDate.HasValue && p.LastPaymentDate.Value >= defaultStartDate && p.LastPaymentDate.Value <= defaultEndDate))
                .ToList();
        }

        return new PendingVsCompletedPaymentsDto 
        {
            PendingAmount = filteredPayments.Where(p => p.Status == PaymentStatus.Pending).Sum(p => p.PendingAmount),
            CompletedAmount = filteredPayments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.AmountPaid)
        };
    }
}
