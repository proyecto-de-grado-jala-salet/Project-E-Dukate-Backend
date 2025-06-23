using E_Dukate.Application.Services.Payments;
using E_Dukate.Application.DTOs.Payments;
using E_Dukate.Domain.Entities.Payments;
using System.Globalization;
using E_Dukate.Application.DTOs.Metrics;

namespace E_Dukate.Application.Services.Metrics;

public class PaymentMetricsService
{
    private readonly PaymentService _paymentService;

    public PaymentMetricsService(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task<List<IncomeByPeriodDto>> GetTotalIncomeByPeriodAsync(string? periodType, DateTime? startDate, DateTime? endDate)
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

        var result = new List<IncomeByPeriodDto>();

        switch (periodType.ToLower())
        {
            case "monthly":
                result = filteredPayments
                    .GroupBy(p => new
                    {
                        Year = p.FirstPaymentDate?.Year ?? p.LastPaymentDate?.Year,
                        Month = p.FirstPaymentDate?.Month ?? p.LastPaymentDate?.Month
                    })
                    .Where(g => g.Key.Year.HasValue && g.Key.Month.HasValue)
                    .Select(g => new IncomeByPeriodDto
                    {
                        Period = $"{g.Key.Year!.Value}-{g.Key.Month!.Value:00}",
                        TotalIncome = g.Sum(p => p.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;

            case "weekly":
                result = filteredPayments
                    .GroupBy(p =>
                    {
                        var date = p.FirstPaymentDate ?? p.LastPaymentDate;
                        if (!date.HasValue) return null;
                        var week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                            date.Value,
                            CalendarWeekRule.FirstFourDayWeek,
                            DayOfWeek.Monday);
                        return new { Year = date.Value.Year, Week = week };
                    })
                    .Where(g => g.Key != null)
                    .Select(g => new IncomeByPeriodDto
                    {
                        Period = $"{g.Key!.Year}-W{g.Key.Week:00}",
                        TotalIncome = g.Sum(p => p.TotalAmount)
                    })
                    .OrderBy(r => r.Period)
                    .ToList();
                break;

            case "yearly":
                result = filteredPayments
                    .GroupBy(p => p.FirstPaymentDate?.Year ?? p.LastPaymentDate?.Year)
                    .Where(g => g.Key.HasValue)
                    .Select(g => new IncomeByPeriodDto
                    {
                        Period = g.Key!.Value.ToString(),
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

    public async Task<PendingVsCompletedPaymentsDto> GetPendingVsCompletedPaymentsAsync()
    {
        var filter = new PaymentFilterDto
        {
            PageNumber = 1,
            PageSize = int.MaxValue
        };

        var (payments, _) = await _paymentService.GetFilteredPaymentsAsync(filter);
        var result = new PendingVsCompletedPaymentsDto
        {
            PendingAmount = payments.Where(p => p.Status == PaymentStatus.Pending).Sum(p => p.PendingAmount),
            CompletedAmount = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.AmountPaid)
        };

        return result;
    }
}
