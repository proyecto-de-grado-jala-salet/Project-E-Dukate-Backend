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

        // Actualizar solo el AmountPaid y recalcular los campos derivados
        payment.AmountPaid = dto.AmountPaid;
        payment.PendingAmount = payment.TotalAmount - dto.AmountPaid;
        payment.LastPaymentDate = payment.AmountPaid > 0 ? (payment.LastPaymentDate ?? DateTime.UtcNow) : payment.LastPaymentDate;

        // Actualizar el estado del pago
        if (payment.AmountPaid >= payment.TotalAmount)
        {
            payment.Status = PaymentStatus.Completed;
            payment.PendingAmount = 0;
            payment.LastPaymentDate = payment.LastPaymentDate ?? DateTime.UtcNow;
        }
        else
        {
            payment.Status = PaymentStatus.Pending;
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

        var activeSessionCount = appointment.ScheduledSessions
            .Count(ss => ss.Status != ScheduledSessionStatus.Cancelled);

        var payment = appointment.Payment;
        payment.SessionCount = activeSessionCount;
        payment.TotalAmount = payment.SessionCost * activeSessionCount;
        payment.PendingAmount = payment.TotalAmount - payment.AmountPaid;
        payment.SpecialistAmount = payment.TotalAmount * 0.5m;
        payment.InstitutionAmount = payment.TotalAmount * 0.5m;

        // Ajustar el monto pagado si excede el total actualizado
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
}