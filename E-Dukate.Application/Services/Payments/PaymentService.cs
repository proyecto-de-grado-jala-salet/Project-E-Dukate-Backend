using E_Dukate.Application.DTOs.Payments;
using E_Dukate.Application.DTOs.Common;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services.Payments;

public class PaymentService
{
    private readonly IGenericRepository<Payment> _paymentRepository;
    private readonly IGenericRepository<Appointment> _appointmentRepository;
    private readonly IValidator<PaymentDto> _validator;

    public PaymentService(
        IGenericRepository<Payment> paymentRepository,
        IGenericRepository<Appointment> appointmentRepository,
        IValidator<PaymentDto> validator)
    {
        _paymentRepository = paymentRepository;
        _appointmentRepository = appointmentRepository;
        _validator = validator;
    }

    public async Task<Result> UpdatePaymentAsync(Guid id, PaymentDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var payment = await _paymentRepository.GetAll()
            .Include(p => p.Appointment)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null)
            return Result.Failure("Pago no encontrado.");

        var newTotalAmount = dto.SessionCost * payment.SessionCount;
        if (dto.AmountPaid > newTotalAmount)
            return Result.Failure($"El monto pagado ({dto.AmountPaid}) no puede ser mayor que el monto total ({newTotalAmount}).");

        if (dto.AmountPaid > 0 && payment.AmountPaid == 0)
        {
            payment.FirstPaymentDate = DateTime.UtcNow;
        }

        payment.AmountPaid = dto.AmountPaid;
        payment.SessionCost = dto.SessionCost;
        payment.TotalAmount = payment.SessionCost * payment.SessionCount;
        payment.PendingAmount = payment.TotalAmount - payment.AmountPaid;
        payment.SpecialistAmount = payment.TotalAmount * 0.5m;
        payment.InstitutionAmount = payment.TotalAmount * 0.5m;

        if (payment.AmountPaid >= payment.TotalAmount)
        {
            payment.Status = PaymentStatus.Completed;
            payment.PendingAmount = 0;
            payment.LastPaymentDate = DateTime.UtcNow;
        }
        else
        {
            payment.Status = PaymentStatus.Pending;
            payment.LastPaymentDate = null;
        }

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<Result> DeletePaymentAsync(Guid id)
    {
        var payment = await _paymentRepository.GetByIdAsync(id);
        if (payment == null)
            return Result.Failure("Pago no encontrado.");

        var appointment = await _appointmentRepository.GetByIdAsync(payment.AppointmentId);
        if (appointment != null)
        {
            appointment.PaymentId = null;
            appointment.Payment = null;
            await _appointmentRepository.UpdateAsync(appointment);
        }

        await _paymentRepository.DeleteAsync(id);
        return Result.Success();
    }

    public async Task<Result> UpdatePaymentOnSessionCancellationAsync(Guid appointmentId)
    {
        var appointment = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appointment == null)
            return Result.Failure("Cita no encontrada.");

        if (appointment.Payment == null)
            return Result.Success();

        var payment = appointment.Payment;
        var activeSessionCount = appointment.ScheduledSessions
            .Count(ss => ss.Status != ScheduledSessionStatus.Cancelled);

        if (payment.SessionCount == activeSessionCount)
            return Result.Success();

        payment.SessionCount = activeSessionCount;
        payment.TotalAmount = payment.SessionCost * activeSessionCount;
        payment.PendingAmount = payment.TotalAmount - payment.AmountPaid;
        payment.SpecialistAmount = payment.TotalAmount * 0.5m;
        payment.InstitutionAmount = payment.TotalAmount * 0.5m;

        if (payment.AmountPaid > payment.TotalAmount)
        {
            payment.AmountPaid = payment.TotalAmount;
            payment.PendingAmount = 0;
            payment.Status = PaymentStatus.Completed;
            payment.LastPaymentDate = payment.LastPaymentDate ?? DateTime.UtcNow;
        }
        else if (payment.AmountPaid < payment.TotalAmount)
        {
            payment.Status = PaymentStatus.Pending;
            payment.LastPaymentDate = null;
        }
        else if (payment.AmountPaid == payment.TotalAmount)
        {
            payment.Status = PaymentStatus.Completed;
            payment.PendingAmount = 0;
            payment.LastPaymentDate = payment.LastPaymentDate ?? DateTime.UtcNow;
        }

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<Payment?> GetPaymentByIdAsync(Guid id)
    {
        return await _paymentRepository.GetAll()
            .Include(p => p.Appointment)
            .Include(p => p.Patient)
            .Include(p => p.Specialist)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(IEnumerable<Payment>, int)> GetPaymentsAsync(PaginationParams pagination)
    {
        var query = _paymentRepository.GetAll()
            .Include(p => p.Appointment)
            .Include(p => p.Patient)
            .Include(p => p.Specialist);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Payment>, int)> GetFilteredPaymentsAsync(PaymentFilterDto filter)
    {
        var query = _paymentRepository.GetAll();

        query = query
            .Include(p => p.Appointment)
            .Include(p => p.Patient)
            .Include(p => p.Specialist);
        
        if (filter.SpecialistId.HasValue)
        {
            query = query.Where(p => p.SpecialistId == filter.SpecialistId.Value);
        }

        if (filter.Status != null && Enum.TryParse<PaymentStatus>(filter.Status, true, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (filter.Year.HasValue)
        {
            query = query.Where(p => (p.FirstPaymentDate.HasValue && p.FirstPaymentDate.Value.Year == filter.Year.Value) ||
                                    (p.LastPaymentDate.HasValue && p.LastPaymentDate.Value.Year == filter.Year.Value));
        }

        if (filter.Month.HasValue)
        {
            query = query.Where(p => (p.FirstPaymentDate.HasValue && p.FirstPaymentDate.Value.Month == filter.Month.Value) ||
                                    (p.LastPaymentDate.HasValue && p.LastPaymentDate.Value.Month == filter.Month.Value));
        }

        if (filter.Day.HasValue)
        {
            query = query.Where(p => (p.FirstPaymentDate.HasValue && p.FirstPaymentDate.Value.Day == filter.Day.Value) ||
                                    (p.LastPaymentDate.HasValue && p.LastPaymentDate.Value.Day == filter.Day.Value));
        }

        var totalCount = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(p => p.FirstPaymentDate ?? p.LastPaymentDate ?? DateTime.MaxValue)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}