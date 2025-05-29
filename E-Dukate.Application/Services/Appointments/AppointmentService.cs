using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Application.DTOs.Common;

namespace E_Dukate.Application.Services.Appointments;

public class AppointmentService : BaseService<Appointment, AppointmentDto>
{
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;
    private readonly IGenericRepository<Specialty> _specialtyRepository;
    private readonly IGenericRepository<Payment> _paymentRepository;

    public AppointmentService(
        IGenericRepository<Appointment> repository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<Specialist> specialistRepository,
        IGenericRepository<Specialty> specialtyRepository,
        IGenericRepository<Payment> paymentRepository,
        IValidator<AppointmentDto> validator)
        : base(repository, validator)
    {
        _patientRepository = patientRepository;
        _specialistRepository = specialistRepository;
        _specialtyRepository = specialtyRepository;
        _paymentRepository = paymentRepository;
    }

    public override Result Register(AppointmentDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        // Verificar existencia de entidades relacionadas
        var patient = _patientRepository.GetById(dto.PatientId);
        if (patient == null)
            return Result.Failure("Paciente no encontrado.");

        var specialist = _specialistRepository.GetById(dto.SpecialistId);
        if (specialist == null)
            return Result.Failure("Especialista no encontrado.");

        var specialty = _specialtyRepository.GetById(dto.SpecialtyId);
        if (specialty == null)
            return Result.Failure("Especialidad no encontrada.");

        // Crear cita
        var appointment = MapToEntity(dto);
        appointment.Patient = patient;
        appointment.Specialist = specialist;
        appointment.Specialty = specialty;
        appointment.Status = AppointmentStatus.Scheduled;

        // Primero guardar la cita para que genere su ID
        Repository.Add(appointment);

        // Ahora crear el pago con el ID de la cita
        var payment = new Payment
        {
            AppointmentId = appointment.Id, // Aquí ya tenemos el ID generado
            PatientId = dto.PatientId,
            SpecialistId = dto.SpecialistId,
            SessionCost = dto.SessionCost,
            SessionCount = dto.SessionCount,
            TotalAmount = dto.SessionCost * dto.SessionCount,
            AmountPaid = 0,
            PendingAmount = dto.SessionCost * dto.SessionCount,
            SpecialistAmount = (dto.SessionCost * dto.SessionCount) / 2,
            InstitutionAmount = (dto.SessionCost * dto.SessionCount) / 2,
            Status = PaymentStatus.Pending
        };

        // Guardar el pago
        _paymentRepository.Add(payment);

        // Actualizar la cita con el ID del pago
        appointment.PaymentId = payment.Id;
        Repository.Update(appointment);

        return Result.Success();
    }

    public override Result Update(Guid id, AppointmentDto dto)
    {
        var validationResult = Validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var appointment = Repository.GetById(id);
        if (appointment == null)
            return Result.Failure("Cita no encontrada.");

        // Verificar existencia de entidades relacionadas
        var patient = _patientRepository.GetById(dto.PatientId);
        if (patient == null)
            return Result.Failure("Paciente no encontrado.");

        var specialist = _specialistRepository.GetById(dto.SpecialistId);
        if (specialist == null)
            return Result.Failure("Especialista no encontrado.");

        var specialty = _specialtyRepository.GetById(dto.SpecialtyId);
        if (specialty == null)
            return Result.Failure("Especialidad no encontrada.");

        // Actualizar cita
        UpdateEntity(appointment, dto);
        appointment.Patient = patient;
        appointment.Specialist = specialist;
        appointment.Specialty = specialty;

        // Actualizar pago asociado
        var payment = _paymentRepository.GetById(appointment.PaymentId ?? Guid.Empty);
        if (payment != null)
        {
            payment.SessionCost = dto.SessionCost;
            payment.SessionCount = dto.SessionCount;
            payment.TotalAmount = dto.SessionCost * dto.SessionCount;
            payment.PendingAmount = payment.TotalAmount - payment.AmountPaid;
            payment.SpecialistAmount = payment.TotalAmount / 2;
            payment.InstitutionAmount = payment.TotalAmount / 2;
            payment.Status = payment.PendingAmount > 0 ? PaymentStatus.Pending : PaymentStatus.Completed;
            _paymentRepository.Update(payment);
        }

        Repository.Update(appointment);
        return Result.Success();
    }

    public override void Delete(Guid id)
    {
        var appointment = Repository.GetById(id);
        if (appointment == null)
            throw new Exception("Cita no encontrada.");

        Repository.Delete(id);
    }

    public override Appointment? FindById(Guid id)
    {
        return Repository.GetAll()
            .Include(a => a.Patient)
            .Include(a => a.Specialist)
            .Include(a => a.Specialty)
            .Include(a => a.Payment)
            .FirstOrDefault(a => a.Id == id);
    }

    public IEnumerable<Appointment> ListAll(Guid? specialistId = null)
    {
        var query = Repository.GetAll();
        if (specialistId.HasValue)
            query = query.Where(a => a.SpecialistId == specialistId.Value);

        return query
            .Include(a => a.Patient)
            .Include(a => a.Specialist)
            .Include(a => a.Specialty)
            .Include(a => a.Payment)
            .ToList();
    }

    public async Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedAsync(PaginationParams pagination, Guid? specialistId = null)
    {
        var query = Repository.GetAll();
        if (specialistId.HasValue)
            query = query.Where(a => a.SpecialistId == specialistId.Value);

        query = query
            .Include(a => a.Patient)
            .Include(a => a.Specialist)
            .Include(a => a.Specialty)
            .Include(a => a.Payment);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();
        return (items, totalCount);
    }

    protected override Appointment MapToEntity(AppointmentDto dto)
    {
        var patient = _patientRepository.GetById(dto.PatientId) ?? throw new Exception("Paciente no encontrado.");
        var specialist = _specialistRepository.GetById(dto.SpecialistId) ?? throw new Exception("Especialista no encontrado.");
        var specialty = _specialtyRepository.GetById(dto.SpecialtyId) ?? throw new Exception("Especialidad no encontrada.");

        return new Appointment
        {
            PatientId = dto.PatientId,
            Patient = patient,
            SpecialistId = dto.SpecialistId,
            Specialist = specialist,
            SpecialtyId = dto.SpecialtyId,
            Specialty = specialty,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            SessionCount = dto.SessionCount
        };
    }

    protected override void UpdateEntity(Appointment entity, AppointmentDto dto)
    {
        var patient = _patientRepository.GetById(dto.PatientId) ?? throw new Exception("Paciente no encontrado.");
        var specialist = _specialistRepository.GetById(dto.SpecialistId) ?? throw new Exception("Especialista no encontrado.");
        var specialty = _specialtyRepository.GetById(dto.SpecialtyId) ?? throw new Exception("Especialidad no encontrada.");

        entity.PatientId = dto.PatientId;
        entity.SpecialistId = dto.SpecialistId;
        entity.SpecialtyId = dto.SpecialtyId;
        entity.StartTime = dto.StartTime;
        entity.EndTime = dto.EndTime;
        entity.SessionCount = dto.SessionCount;
        entity.Patient = patient;
        entity.Specialist = specialist;
        entity.Specialty = specialty;
    }
}