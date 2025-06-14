using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Application.DTOs.Common;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Application.Services.Payments;

namespace E_Dukate.Application.Services.Appointments;

public class AppointmentService
{
    private readonly IGenericRepository<Appointment> _appointmentRepository;
    private readonly IGenericRepository<ScheduledSession> _scheduledSessionRepository;
    private readonly IGenericRepository<Patient> _patientRepository;
    private readonly IGenericRepository<Specialist> _specialistRepository;
    private readonly IGenericRepository<Specialty> _specialtyRepository;
    private readonly IGenericRepository<Schedule> _scheduleRepository;
    private readonly IGenericRepository<TimeSlot> _timeSlotRepository;
    private readonly IGenericRepository<Payment> _paymentRepository;
    private readonly PaymentService _paymentService;
    private readonly IValidator<AppointmentDto> _validator;

    public AppointmentService(
        IGenericRepository<Appointment> appointmentRepository,
        IGenericRepository<ScheduledSession> scheduledSessionRepository,
        IGenericRepository<Patient> patientRepository,
        IGenericRepository<Specialist> specialistRepository,
        IGenericRepository<Specialty> specialtyRepository,
        IGenericRepository<Schedule> scheduleRepository,
        IGenericRepository<TimeSlot> timeSlotRepository,
        IGenericRepository<Payment> paymentRepository,
        PaymentService paymentService,
        IValidator<AppointmentDto> validator)
    {
        _appointmentRepository = appointmentRepository;
        _scheduledSessionRepository = scheduledSessionRepository;
        _patientRepository = patientRepository;
        _specialistRepository = specialistRepository;
        _specialtyRepository = specialtyRepository;
        _scheduleRepository = scheduleRepository;
        _timeSlotRepository = timeSlotRepository;
        _paymentRepository = paymentRepository;
        _paymentService = paymentService;
        _validator = validator;
    }

    public async Task<Result> CreateAppointmentAsync(AppointmentDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
        if (patient == null)
            return Result.Failure("Paciente no encontrado.");

        var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
        if (specialty == null)
            return Result.Failure("Especialidad no encontrada.");

        var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
        if (specialist == null)
            return Result.Failure("Especialista no encontrado.");

        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
            .ToListAsync();

        foreach (var sessionDto in dto.ScheduledSessions)
        {
            if (!Enum.TryParse<ScheduledSessionStatus>(sessionDto.Status, true, out _))
                return Result.Failure($"Estado de sesión inválido: {sessionDto.Status}.");

            var timeSlot = await _timeSlotRepository.GetByIdAsync(sessionDto.TimeSlotId);
            if (timeSlot == null)
                return Result.Failure($"Horario no encontrado para la sesión en {sessionDto.SessionDateTime}.");

            var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId);
            if (schedule == null || schedule.DayOfWeek != sessionDto.SessionDateTime.DayOfWeek)
                return Result.Failure($"El horario seleccionado no corresponde al día {sessionDto.SessionDateTime.DayOfWeek}.");

            var existingSession = await _scheduledSessionRepository.GetAll()
                .AnyAsync(ss => ss.TimeSlotId == sessionDto.TimeSlotId &&
                                ss.SessionDateTime.Date == sessionDto.SessionDateTime.Date);
            if (existingSession)
                return Result.Failure($"El horario {sessionDto.SessionDateTime} ya está ocupado.");
        }

        var appointment = new Appointment
        {
            PatientId = dto.PatientId,
            Patient = patient,
            SpecialtyId = dto.SpecialtyId,
            Specialty = specialty,
            SpecialistId = dto.SpecialistId,
            Specialist = specialist,
            SessionCount = dto.SessionCount,
            ScheduledSessions = dto.ScheduledSessions.Select(ss => new ScheduledSession
            {
                TimeSlotId = ss.TimeSlotId,
                SessionDateTime = ss.SessionDateTime,
                Status = Enum.Parse<ScheduledSessionStatus>(ss.Status, true)
            }).ToList()
        };

        // Crear el Payment automáticamente
        const decimal defaultSessionCost = 65.0m; // 65 bs por sesión
        var payment = new Payment
        {
            Appointment = appointment,
            PatientId = dto.PatientId,
            Patient = patient,
            SpecialistId = dto.SpecialistId,
            Specialist = specialist,
            SessionCost = defaultSessionCost,
            SessionCount = dto.SessionCount,
            TotalAmount = defaultSessionCost * dto.SessionCount,
            AmountPaid = 0, // Inicialmente sin pago
            PendingAmount = defaultSessionCost * dto.SessionCount,
            SpecialistAmount = (defaultSessionCost * dto.SessionCount) * 0.5m,
            InstitutionAmount = (defaultSessionCost * dto.SessionCount) * 0.5m,
            FirstPaymentDate = null,
            LastPaymentDate = null,
            Status = PaymentStatus.Pending
        };

        appointment.Payment = payment;
        appointment.PaymentId = payment.Id;

        await _appointmentRepository.AddAsync(appointment);
        await _paymentRepository.AddAsync(payment);

        return Result.Success();
    }

    public async Task<Result> UpdateAppointmentAsync(Guid id, AppointmentDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
            return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var appointment = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            return Result.Failure("Cita no encontrada.");

        var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
        if (patient == null)
            return Result.Failure("Paciente no encontrado.");

        var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
        if (specialty == null)
            return Result.Failure("Especialidad no encontrada.");

        var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
        if (specialist == null)
            return Result.Failure("Especialista no encontrado.");

        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
            .ToListAsync();

        foreach (var sessionDto in dto.ScheduledSessions)
        {
            if (!Enum.TryParse<ScheduledSessionStatus>(sessionDto.Status, true, out _))
                return Result.Failure($"Estado de sesión inválido: {sessionDto.Status}.");

            var timeSlot = await _timeSlotRepository.GetByIdAsync(sessionDto.TimeSlotId);
            if (timeSlot == null)
                return Result.Failure($"Horario no encontrado para la sesión en {sessionDto.SessionDateTime}.");

            var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId);
            if (schedule == null || schedule.DayOfWeek != sessionDto.SessionDateTime.DayOfWeek)
                return Result.Failure($"El horario seleccionado no corresponde al día {sessionDto.SessionDateTime.DayOfWeek}.");

            var existingSession = await _scheduledSessionRepository.GetAll()
                .AnyAsync(ss => ss.TimeSlotId == sessionDto.TimeSlotId &&
                                ss.SessionDateTime.Date == sessionDto.SessionDateTime.Date &&
                                ss.AppointmentId != id);
            if (existingSession)
                return Result.Failure($"El horario {sessionDto.SessionDateTime} ya está ocupado.");
        }

        // Eliminar sesiones existentes
        foreach (var session in appointment.ScheduledSessions.ToList())
        {
            await _scheduledSessionRepository.DeleteAsync(session.Id);
        }

        // Actualizar cita
        appointment.PatientId = dto.PatientId;
        appointment.Patient = patient;
        appointment.SpecialtyId = dto.SpecialtyId;
        appointment.Specialty = specialty;
        appointment.SpecialistId = dto.SpecialistId;
        appointment.Specialist = specialist;
        appointment.SessionCount = dto.SessionCount;
        appointment.ScheduledSessions = dto.ScheduledSessions.Select(ss => new ScheduledSession
        {
            TimeSlotId = ss.TimeSlotId,
            SessionDateTime = ss.SessionDateTime,
            Status = Enum.Parse<ScheduledSessionStatus>(ss.Status, true)
        }).ToList();

        // Actualizar el Payment asociado
        if (appointment.Payment != null)
        {
            appointment.Payment.SessionCount = dto.SessionCount;
            appointment.Payment.TotalAmount = appointment.Payment.SessionCost * dto.SessionCount;
            appointment.Payment.PendingAmount = appointment.Payment.TotalAmount - appointment.Payment.AmountPaid;
            appointment.Payment.SpecialistAmount = appointment.Payment.TotalAmount * 0.5m;
            appointment.Payment.InstitutionAmount = appointment.Payment.TotalAmount * 0.5m;

            if (appointment.Payment.AmountPaid >= appointment.Payment.TotalAmount)
            {
                appointment.Payment.Status = PaymentStatus.Completed;
                appointment.Payment.PendingAmount = 0;
                appointment.Payment.LastPaymentDate = appointment.Payment.LastPaymentDate ?? DateTime.UtcNow;
            }
            else
            {
                appointment.Payment.Status = PaymentStatus.Pending;
            }

            await _paymentRepository.UpdateAsync(appointment.Payment);
        }

        await _appointmentRepository.UpdateAsync(appointment);
        return Result.Success();
    }

    public async Task<Result> DeleteAppointmentAsync(Guid id)
    {
        var appointment = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            return Result.Failure("Cita no encontrada.");

        foreach (var session in appointment.ScheduledSessions)
        {
            session.Status = ScheduledSessionStatus.Cancelled;
            await _scheduledSessionRepository.UpdateAsync(session);
        }

        if (appointment.Payment != null)
        {
            var paymentResult = await _paymentService.UpdatePaymentOnSessionCancellationAsync(id);
            if (!paymentResult.IsSuccess)
                return paymentResult;
        }

        await _appointmentRepository.DeleteAsync(id);
        return Result.Success();
    }

    public async Task<Result> ConfirmSessionAsync(Guid sessionId, Guid patientId)
    {
        var session = await _scheduledSessionRepository.GetAll()
            .Include(ss => ss.Appointment)
            .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(ss => ss.Id == sessionId);
        if (session == null)
            return Result.Failure("Sesión no encontrada.");

        if (session.AppointmentId != patientId)
            return Result.Failure("No tienes permiso para confirmar esta sesión.");

        if (session.Status == ScheduledSessionStatus.Cancelled)
            return Result.Failure("No puedes confirmar una sesión cancelada.");

        session.Status = ScheduledSessionStatus.Confirmed;
        await _scheduledSessionRepository.UpdateAsync(session);
        return Result.Success();
    }

    public async Task<Result> CancelSessionAsync(Guid sessionId, Guid patientId)
    {
        var session = await _scheduledSessionRepository.GetAll()
            .Include(ss => ss.Appointment)
            .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(ss => ss.Id == sessionId);
        if (session == null)
            return Result.Failure("Sesión no encontrada.");

        if (session.Appointment.PatientId != patientId)
            return Result.Failure("No tienes permiso para cancelar esta sesión.");

        if (session.Status == ScheduledSessionStatus.Cancelled)
            return Result.Failure("La sesión ya está cancelada.");

        session.Status = ScheduledSessionStatus.Cancelled;
        await _scheduledSessionRepository.UpdateAsync(session);

        var paymentResult = await _paymentService.UpdatePaymentOnSessionCancellationAsync(session.AppointmentId);
        if (!paymentResult.IsSuccess)
            return paymentResult;

        return Result.Success();
    }

    public async Task<Result> RescheduleSessionAsync(Guid sessionId, Guid patientId, ScheduledSessionDto dto)
    {
        var session = await _scheduledSessionRepository.GetAll()
            .Include(ss => ss.Appointment)
            .ThenInclude(a => a.Specialist)
            .FirstOrDefaultAsync(ss => ss.Id == sessionId);
        if (session == null)
            return Result.Failure("Sesión no encontrada.");

        if (session.Appointment.PatientId != patientId)
            return Result.Failure("No tienes permiso para reprogramar esta sesión.");

        var timeSlot = await _timeSlotRepository.GetByIdAsync(dto.TimeSlotId);
        if (timeSlot == null)
            return Result.Failure($"Horario no encontrado para la sesión en {dto.SessionDateTime}.");

        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == session.Appointment.SpecialistId && s.Attends)
            .ToListAsync();

        var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId);
        if (schedule == null || schedule.DayOfWeek != dto.SessionDateTime.DayOfWeek)
            return Result.Failure($"El horario seleccionado no corresponde al día {dto.SessionDateTime.DayOfWeek}.");

        var existingSession = await _scheduledSessionRepository.GetAll()
            .AnyAsync(ss => ss.TimeSlotId == dto.TimeSlotId &&
                            ss.SessionDateTime.Date == dto.SessionDateTime.Date &&
                            ss.Id != sessionId);
        if (existingSession)
            return Result.Failure($"El horario {dto.SessionDateTime} ya está ocupado.");

        session.TimeSlotId = dto.TimeSlotId;
        session.SessionDateTime = dto.SessionDateTime;
        session.Status = ScheduledSessionStatus.Rescheduled;
        await _scheduledSessionRepository.UpdateAsync(session);

        return Result.Success();
    }

    private string GetAppointmentStatus(Appointment appointment)
    {
        var sessionStatuses = appointment.ScheduledSessions.Select(ss => ss.Status).ToList();
        if (!sessionStatuses.Any())
            return "Scheduled";
        if (sessionStatuses.All(s => s == ScheduledSessionStatus.Confirmed))
            return "Confirmed";
        if (sessionStatuses.All(s => s == ScheduledSessionStatus.Cancelled))
            return "Cancelled";
        if (sessionStatuses.Any(s => s == ScheduledSessionStatus.Rescheduled))
            return "Rescheduled";
        return "Scheduled";
    }

    public async Task<object?> GetAppointmentByIdAsync(Guid id)
    {
        var appointment = await _appointmentRepository.GetAll()
            .Include(a => a.Patient)
            .Include(a => a.Specialty)
            .Include(a => a.Specialist)
            .Include(a => a.ScheduledSessions)
            .ThenInclude(ss => ss.TimeSlot)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return null;

        return new
        {
            appointment.Id,
            appointment.PatientId,
            appointment.SpecialtyId,
            appointment.SpecialistId,
            appointment.SessionCount,
            ScheduledSessions = appointment.ScheduledSessions.Select(ss => new
            {
                ss.Id,
                ss.TimeSlotId,
                ss.SessionDateTime,
                Status = ss.Status.ToString()
            }),
            PaymentId = appointment.PaymentId,
            Status = GetAppointmentStatus(appointment)
        };
    }

    public async Task<(IEnumerable<object>, int)> GetAppointmentsAsync(PaginationParams pagination)
    {
        var query = _appointmentRepository.GetAll()
            .Include(a => a.Patient)
            .Include(a => a.Specialty)
            .Include(a => a.Specialist)
            .Include(a => a.ScheduledSessions)
            .ThenInclude(ss => ss.TimeSlot);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var result = items.Select(a => new
        {
            a.Id,
            a.PatientId,
            a.SpecialtyId,
            a.SpecialistId,
            a.SessionCount,
            ScheduledSessions = a.ScheduledSessions.Select(ss => new
            {
                ss.Id,
                ss.TimeSlotId,
                ss.SessionDateTime,
                Status = ss.Status.ToString()
            }),
            a.PaymentId,
            Status = GetAppointmentStatus(a)
        });

        return (result, totalCount);
    }
}