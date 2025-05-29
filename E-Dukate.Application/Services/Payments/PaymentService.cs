using E_Dukate.Application.DTOs.Common;
using E_Dukate.Application.DTOs.Payments;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Application.Services.Payments;

public class PaymentService : BaseService<Payment, PaymentDto>
{
    private readonly IGenericRepository<Payment> _paymentRepository;

    public PaymentService(
        IGenericRepository<Payment> repository,
        IValidator<PaymentDto> validator)
        : base(repository, validator)
    {
        _paymentRepository = repository;
    }

    public Result UpdatePaymentAmount(Guid id, decimal totalAmountPaid)
    {
        var payment = _paymentRepository.GetById(id);
        if (payment == null)
            return Result.Failure("Pago no encontrado.");

        if (totalAmountPaid < 0 || totalAmountPaid > payment.TotalAmount)
            return Result.Failure("El monto pagado es inválido.");

        // Establecemos el monto pagado directamente (no sumamos)
        payment.AmountPaid = totalAmountPaid;
        payment.PendingAmount = payment.TotalAmount - payment.AmountPaid;
        payment.Status = payment.PendingAmount == 0 ? PaymentStatus.Completed : PaymentStatus.Pending;

        // Actualizamos las fechas
        if (payment.FirstPaymentDate == null && totalAmountPaid > 0)
            payment.FirstPaymentDate = DateTime.UtcNow;

        if (totalAmountPaid > 0)
            payment.LastPaymentDate = DateTime.UtcNow;

        _paymentRepository.Update(payment);
        return Result.Success();
    }

    public override Payment? FindById(Guid id)
    {
        return _paymentRepository.GetAll()
            .Include(p => p.Appointment)
            .Include(p => p.Patient)
            .Include(p => p.Specialist)
            .FirstOrDefault(p => p.Id == id);
    }

    // public override IEnumerable<Payment> ListAll()
    // {
    //     var query = _paymentRepository.GetAll();
    //     query = query.Include(p => p.Appointment);
    //     query = query.Include(p => p.Patient);
    //     query = query.Include(p => p.Specialist);

    //     return query.ToList();
    // }

    public IEnumerable<Payment> ListAll(Guid? specialistId = null)
    {
        var query = _paymentRepository.GetAll();
        if (specialistId.HasValue)
            query = query.Where(p => p.SpecialistId == specialistId.Value);

        return query
            .Include(p => p.Appointment)
            .Include(p => p.Patient)
            .Include(p => p.Specialist)
            .ToList();
    }

    // public override async Task<(IEnumerable<Payment> Items, int TotalCount)> GetPagedAsync(PaginationParams pagination)
    // {
    //     var query = _paymentRepository.GetAll();
    //     query = query.Include(p => p.Appointment);
    //     query = query.Include(p => p.Patient);
    //     query = query.Include(p => p.Specialist);

    //     var totalCount = await query.CountAsync();
    //     var items = await query
    //         .Skip((pagination.PageNumber - 1) * pagination.PageSize)
    //         .Take(pagination.PageSize)
    //         .ToListAsync();
    //     return (items, totalCount);
    // }

    public async Task<(IEnumerable<Payment> Items, int TotalCount)> GetPagedAsync(PaginationParams pagination, Guid? specialistId = null)
    {
        var query = _paymentRepository.GetAll();
        if (specialistId.HasValue)
            query = query.Where(p => p.SpecialistId == specialistId.Value);

        query = query
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

    protected override Payment MapToEntity(PaymentDto dto)
    {
        throw new NotImplementedException("Creación de pagos se maneja automáticamente con citas.");
    }

    protected override void UpdateEntity(Payment entity, PaymentDto dto)
    {
        throw new NotImplementedException("Actualización de pagos se maneja mediante UpdatePaymentAmount.");
    }
}