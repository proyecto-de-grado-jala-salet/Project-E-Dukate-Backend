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
            return Result.Failure("Patient not found.");

        var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
        if (specialty == null)
            return Result.Failure("Specialty not found.");

        var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
        if (specialist == null)
            return Result.Failure("Specialist not found.");

        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
            .ToListAsync();

        var scheduledSessions = new List<ScheduledSession>();
        var currentDate = DateTime.UtcNow.Date;
        var sessionsAssigned = 0;

        var sessionsPerSlot = dto.ScheduledSessions.Count > 0
            ? (int)Math.Ceiling((double)dto.SessionCount / dto.ScheduledSessions.Count)
            : 0;

        foreach (var sessionDto in dto.ScheduledSessions)
        {
            if (!Enum.TryParse<DayOfWeek>(sessionDto.DayOfWeek, true, out var dayOfWeek))
                return Result.Failure($"Invalid day of the week: {sessionDto.DayOfWeek}.");

            if (!TimeOnly.TryParse(sessionDto.StartTime, out var startTime) ||
                !TimeOnly.TryParse(sessionDto.EndTime, out var endTime))
                return Result.Failure($"Invalid time format: {sessionDto.StartTime} - {sessionDto.EndTime}.");

            var timeSlot = await _timeSlotRepository.GetAll()
                .FirstOrDefaultAsync(ts => ts.Id == sessionDto.TimeSlotId &&
                                           ts.StartTime == startTime &&
                                           ts.EndTime == endTime);
            if (timeSlot == null)
                return Result.Failure($"Schedule not found: {sessionDto.StartTime} - {sessionDto.EndTime}.");

            var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId && s.DayOfWeek == dayOfWeek);
            if (schedule == null)
                return Result.Failure($"The schedule is not available for {sessionDto.DayOfWeek}.");

            var daysUntilNext = ((int)dayOfWeek - (int)currentDate.DayOfWeek + 7) % 7;
            var todayAtStartTime = currentDate.Add(startTime.ToTimeSpan());

            if (daysUntilNext == 0 && DateTime.UtcNow > todayAtStartTime)
            {
                daysUntilNext = 7;
            }

            var sessionDate = currentDate.AddDays(daysUntilNext);

            Console.WriteLine($"Day calc: Target={dayOfWeek}, Current={currentDate.DayOfWeek}");
            Console.WriteLine($"DaysUntilNext: {daysUntilNext}");
            Console.WriteLine($"Time check: Now={DateTime.UtcNow}, SlotStart={todayAtStartTime}");
            var sessionsToAssign = Math.Min(sessionsPerSlot, dto.SessionCount - sessionsAssigned);
            var maxAttempts = 100;

            for (int i = 0; i < sessionsToAssign && sessionsAssigned < dto.SessionCount && maxAttempts > 0; i++)
            {
                var startSessionDateTime = sessionDate.Add(startTime.ToTimeSpan());
                var endSessionDateTime = sessionDate.Add(endTime.ToTimeSpan());
                var isOccupied = await _scheduledSessionRepository.GetAll()
                    .AnyAsync(ss => ss.TimeSlotId == sessionDto.TimeSlotId &&
                                    ss.StartSessionDateTime.Date == sessionDate &&
                                    ss.StartSessionDateTime.Hour == startTime.Hour &&
                                    ss.StartSessionDateTime.Minute == startTime.Minute);
                if (!isOccupied)
                {
                    scheduledSessions.Add(new ScheduledSession
                    {
                        TimeSlotId = sessionDto.TimeSlotId,
                        StartSessionDateTime = startSessionDateTime,
                        EndSessionDateTime = endSessionDateTime,
                        Status = Enum.Parse<ScheduledSessionStatus>(sessionDto.Status, true)
                    });
                    sessionsAssigned++;
                    sessionDate = sessionDate.AddDays(7);
                }
                else
                {
                    sessionDate = sessionDate.AddDays(7);
                    i--;
                    maxAttempts--;
                }
            }

            if (maxAttempts == 0)
                return Result.Failure($"No dates were found available for {sessionDto.DayOfWeek} at the requested time.");
        }

        if (sessionsAssigned < dto.SessionCount)
            return Result.Failure("Not all requested sessions could be scheduled due to scheduling conflicts.");

        var appointment = new Appointment
        {
            PatientId = dto.PatientId,
            Patient = patient,
            SpecialtyId = dto.SpecialtyId,
            Specialty = specialty,
            SpecialistId = dto.SpecialistId,
            Specialist = specialist,
            SessionCount = dto.SessionCount,
            ScheduledSessions = scheduledSessions
        };

        var payment = new Payment
        {
            Appointment = appointment,
            PatientId = dto.PatientId,
            Patient = patient,
            SpecialistId = dto.SpecialistId,
            Specialist = specialist,
            SessionCost = dto.SessionCost > 0 ? dto.SessionCost : 65.0m,
            SessionCount = dto.SessionCount,
            TotalAmount = (dto.SessionCost > 0 ? dto.SessionCost : 65.0m) * dto.SessionCount,
            AmountPaid = 0,
            PendingAmount = (dto.SessionCost > 0 ? dto.SessionCost : 65.0m) * dto.SessionCount,
            SpecialistAmount = ((dto.SessionCost > 0 ? dto.SessionCost : 65.0m) * dto.SessionCount) * 0.5m,
            InstitutionAmount = ((dto.SessionCost > 0 ? dto.SessionCost : 65.0m) * dto.SessionCount) * 0.5m,
            FirstPaymentDate = null,
            LastPaymentDate = null,
            Status = PaymentStatus.Pending
        };

        appointment.Payment = payment;
        appointment.PaymentId = payment.Id;

        await _appointmentRepository.AddAsync(appointment);

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
            return Result.Failure("Appointment not found.");

        var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
        if (patient == null)
            return Result.Failure("Patient not found.");

        var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
        if (specialty == null)
            return Result.Failure("Specialty not found.");

        var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
        if (specialist == null)
            return Result.Failure("Specialist not found.");

        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
            .ToListAsync();

        var scheduledSessions = new List<ScheduledSession>();
        var currentDate = DateTime.UtcNow.Date;
        var sessionsAssigned = 0;

        var sessionsPerSlot = dto.ScheduledSessions.Count > 0
            ? (int)Math.Ceiling((double)dto.SessionCount / dto.ScheduledSessions.Count)
            : 0;

        foreach (var sessionDto in dto.ScheduledSessions)
        {
            if (!Enum.TryParse<DayOfWeek>(sessionDto.DayOfWeek, true, out var dayOfWeek))
                return Result.Failure($"Invalid day of the week: {sessionDto.DayOfWeek}.");

            if (!TimeOnly.TryParse(sessionDto.StartTime, out var startTime) ||
                !TimeOnly.TryParse(sessionDto.EndTime, out var endTime))
                return Result.Failure($"Invalid time format: {sessionDto.StartTime} - {sessionDto.EndTime}.");

            var timeSlot = await _timeSlotRepository.GetAll()
                .FirstOrDefaultAsync(ts => ts.Id == sessionDto.TimeSlotId &&
                                           ts.StartTime == startTime &&
                                           ts.EndTime == endTime);
            if (timeSlot == null)
                return Result.Failure($"Schedule not found: {sessionDto.StartTime} - {sessionDto.EndTime}.");

            var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId && s.DayOfWeek == dayOfWeek);
            if (schedule == null)
                return Result.Failure($"The schedule is not available for {sessionDto.DayOfWeek}.");

            var daysUntilNext = ((int)dayOfWeek - (int)currentDate.DayOfWeek + 7) % 7;
            if (daysUntilNext == 0 && TimeOnly.FromDateTime(DateTime.UtcNow) > startTime)
                daysUntilNext = 7;

            var sessionDate = currentDate.AddDays(daysUntilNext);
            var sessionsToAssign = Math.Min(sessionsPerSlot, dto.SessionCount - sessionsAssigned);
            var maxAttempts = 10;

            for (int i = 0; i < sessionsToAssign && sessionsAssigned < dto.SessionCount && maxAttempts > 0; i++)
            {
                var startSessionDateTime = sessionDate.Add(startTime.ToTimeSpan());
                var endSessionDateTime = sessionDate.Add(endTime.ToTimeSpan());
                var isOccupied = await _scheduledSessionRepository.GetAll()
                    .AnyAsync(ss => ss.TimeSlotId == sessionDto.TimeSlotId &&
                                    ss.StartSessionDateTime.Date == sessionDate &&
                                    ss.StartSessionDateTime.Hour == startTime.Hour &&
                                    ss.StartSessionDateTime.Minute == startTime.Minute &&
                                    ss.AppointmentId != id);
                if (!isOccupied)
                {
                    scheduledSessions.Add(new ScheduledSession
                    {
                        TimeSlotId = sessionDto.TimeSlotId,
                        StartSessionDateTime = startSessionDateTime,
                        EndSessionDateTime = endSessionDateTime,
                        Status = Enum.Parse<ScheduledSessionStatus>(sessionDto.Status, true)
                    });
                    sessionsAssigned++;
                    sessionDate = sessionDate.AddDays(7);
                }
                else
                {
                    sessionDate = sessionDate.AddDays(7);
                    i--;
                    maxAttempts--;
                }
            }

            if (maxAttempts == 0)
                return Result.Failure($"No dates were found available for {sessionDto.DayOfWeek} at the requested time.");
        }

        if (sessionsAssigned < dto.SessionCount)
            return Result.Failure("Not all requested sessions could be scheduled due to scheduling conflicts.");

        foreach (var session in appointment.ScheduledSessions.ToList())
        {
            await _scheduledSessionRepository.DeleteAsync(session.Id);
        }

        appointment.PatientId = dto.PatientId;
        appointment.Patient = patient;
        appointment.SpecialtyId = dto.SpecialtyId;
        appointment.Specialty = specialty;
        appointment.SpecialistId = dto.SpecialistId;
        appointment.Specialist = specialist;
        appointment.SessionCount = dto.SessionCount;
        appointment.ScheduledSessions = scheduledSessions;

        if (appointment.Payment != null)
        {
            appointment.Payment.SessionCount = dto.SessionCount;
            appointment.Payment.SessionCost = dto.SessionCost > 0 ? dto.SessionCost : 65.0m;
            appointment.Payment.TotalAmount = (dto.SessionCost > 0 ? dto.SessionCost : 65.0m) * dto.SessionCount;
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
                appointment.Payment.LastPaymentDate = null;
            }

            await _paymentRepository.UpdateAsync(appointment.Payment);
        }

        await _appointmentRepository.UpdateAsync(appointment);
        return Result.Success();
    }

    public async Task<Result> ConfirmSessionAsync(Guid sessionId, Guid patientId)
    {
        var session = await _scheduledSessionRepository.GetAll()
            .Include(ss => ss.Appointment)
            .ThenInclude(a => a!.Patient)
            .FirstOrDefaultAsync(ss => ss.Id == sessionId);
        if (session == null)
            return Result.Failure("Session not found.");

        if (session.AppointmentId != patientId)
            return Result.Failure("You do not have permission to confirm this session.");

        if (session.Status == ScheduledSessionStatus.Cancelled)
            return Result.Failure("You cannot confirm a cancelled session.");

        session.Status = ScheduledSessionStatus.Confirmed;
        await _scheduledSessionRepository.UpdateAsync(session);
        return Result.Success();
    }

    public async Task<Result> CancelSessionAsync(Guid appointmentId, Guid sessionId)
    {
        var appointment = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return Result.Failure("Cita no encontrada.");

        if (appointment.Payment == null)
            return Result.Failure("La cita no tiene pago asociado.");

        var session = appointment.ScheduledSessions.FirstOrDefault(s => s.Id == sessionId);
        if (session == null)
            return Result.Failure("Sesión no encontrada.");

        if (session.Status == ScheduledSessionStatus.Cancelled)
            return Result.Failure("La sesión ya está cancelada.");
        
        if (session.StartSessionDateTime <= DateTime.UtcNow)
            return Result.Failure("No se puede cancelar una sesión pasada.");
        
        session.Status = ScheduledSessionStatus.Cancelled;

        var activeSessions = appointment.ScheduledSessions
            .Count(s => s.Status != ScheduledSessionStatus.Cancelled);

        var newTotalAmount = appointment.Payment.SessionCost * activeSessions;

        var originalAmountPaid = appointment.Payment.AmountPaid;

        appointment.Payment.SessionCount = activeSessions;
        appointment.Payment.TotalAmount = newTotalAmount;

        appointment.Payment.AmountPaid = originalAmountPaid;

        appointment.Payment.PendingAmount = Math.Max(0, newTotalAmount - originalAmountPaid);

        appointment.Payment.Status = appointment.Payment.PendingAmount == 0
            ? PaymentStatus.Completed
            : PaymentStatus.Pending;
        
        appointment.Payment.SpecialistAmount = newTotalAmount * 0.5m;
        appointment.Payment.InstitutionAmount = newTotalAmount * 0.5m;

        await _appointmentRepository.UpdateAsync(appointment);

        return Result.Success();
    }

    public async Task<Result> RescheduleSessionAsync(Guid appointmentId, RescheduleSessionDto dto)
    {
        if (!Enum.TryParse<DayOfWeek>(dto.DayOfWeek, true, out var dayOfWeek))
            return Result.Failure($"Día de la semana inválido: {dto.DayOfWeek}.");

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime) ||
            !TimeOnly.TryParse(dto.EndTime, out var endTime))
            return Result.Failure($"Formato de hora inválido: {dto.StartTime} - {dto.EndTime}.");
        
        var appointment = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment == null)
            return Result.Failure("Cita no encontrada.");

        var session = appointment.ScheduledSessions.FirstOrDefault(s => s.Id == dto.SessionId);
        if (session == null)
            return Result.Failure("Sesión no encontrada.");
        
        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == appointment.SpecialistId && s.Attends)
            .ToListAsync();

        var timeSlot = await _timeSlotRepository.GetByIdAsync(dto.TimeSlotId);
        if (timeSlot == null || timeSlot.StartTime != startTime || timeSlot.EndTime != endTime)
            return Result.Failure("Horario no válido.");

        var schedule = schedules.FirstOrDefault(s =>
            s.Id == timeSlot.ScheduleId && s.DayOfWeek == dayOfWeek);

        if (schedule == null)
            return Result.Failure($"El horario no está disponible para {dto.DayOfWeek}.");
        
        var currentDate = DateTime.UtcNow.Date;
        var daysUntilNext = ((int)dayOfWeek - (int)currentDate.DayOfWeek + 7) % 7;
        var todayAtStartTime = currentDate.Add(startTime.ToTimeSpan());

        if (daysUntilNext == 0 && DateTime.UtcNow > todayAtStartTime)
            daysUntilNext = 7;

        var newDate = currentDate.AddDays(daysUntilNext);
        var newStartDateTime = newDate.Add(startTime.ToTimeSpan());
        var newEndDateTime = newDate.Add(endTime.ToTimeSpan());

        var isOccupied = await _scheduledSessionRepository.GetAll()
            .AnyAsync(ss =>
                ss.TimeSlotId == dto.TimeSlotId &&
                ss.StartSessionDateTime.Date == newDate.Date &&
                ss.StartSessionDateTime.Hour == startTime.Hour &&
                ss.StartSessionDateTime.Minute == startTime.Minute &&
                ss.Id != dto.SessionId);

        if (isOccupied)
            return Result.Failure("El horario seleccionado no está disponible.");
        
        session.StartSessionDateTime = newStartDateTime;
        session.EndSessionDateTime = newEndDateTime;
        session.Status = ScheduledSessionStatus.Rescheduled;

        await _appointmentRepository.UpdateAsync(appointment);

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
                ss.StartSessionDateTime,
                ss.EndSessionDateTime,
                Status = ss.Status.ToString()
            }),
            PaymentId = appointment.PaymentId,
            Status = GetAppointmentStatus(appointment)
        };
    }

    public async Task<ValueResult<(IEnumerable<object>, int)>> GetAppointmentsAsync(
    Guid? patientId,
    Guid? specialistId,
    DateTime? date,
    string? status,
    string? patientSearch,
    PaginationParams pagination)
    {
        var query = _appointmentRepository.GetAll()
            .Include(a => a.Patient)
            .Include(a => a.Specialty)
            .Include(a => a.Specialist)
            .Include(a => a.ScheduledSessions)
                .ThenInclude(ss => ss.TimeSlot)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(a => a.PatientId == patientId.Value);
        }

        if (specialistId.HasValue)
        {
            query = query.Where(a => a.SpecialistId == specialistId.Value);
        }

        DateTime filterDate;
        if (date.HasValue)
        {
            filterDate = DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc);
        }
        else
        {
            filterDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        }

        query = query.Where(a => a.ScheduledSessions.Any(ss =>
            DateTime.SpecifyKind(ss.StartSessionDateTime, DateTimeKind.Utc).Date == filterDate));
        
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ScheduledSessionStatus>(status, true, out var parsedStatus))
            {
                return ValueResult<(IEnumerable<object>, int)>.Failure("Estado inválido. Los valores permitidos son: Scheduled, Confirmed, Cancelled, Rescheduled.");
            }

            query = query.Where(a => a.ScheduledSessions.Any(ss => ss.Status == parsedStatus));
        }

        if (!string.IsNullOrWhiteSpace(patientSearch))
        {
            query = query.Where(a => (a.Patient.Names + " " + a.Patient.LastNamePaternal + " " + (a.Patient.LastNameMaternal ?? ""))
                .ToLower().Contains(patientSearch.ToLower()));
        }

        var totalCount = await query.CountAsync();

        var appointments = await query
            .OrderBy(a => a.ScheduledSessions.FirstOrDefault()!.StartSessionDateTime)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();
        
        var result = new List<object>();

        foreach (var appointment in appointments)
        {
            foreach (var session in appointment.ScheduledSessions)
            {
                var sessionDate = DateTime.SpecifyKind(session.StartSessionDateTime, DateTimeKind.Utc).Date;
                if (sessionDate != filterDate)
                    continue;
                
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (session.Status.ToString() != status)
                        continue;
                }

                result.Add(new
                {
                    appointment.Id,
                    StartTime = session.StartSessionDateTime.ToString("HH:mm"),
                    EndTime = session.EndSessionDateTime.ToString("HH:mm"),
                    appointment.PatientId,
                    PatientName = $"{appointment.Patient.Names} {appointment.Patient.LastNamePaternal} {(appointment.Patient.LastNameMaternal ?? "")}".Trim(),
                    appointment.SpecialistId,
                    SpecialistName = $"{appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal} {(appointment.Specialist.LastNameMaternal ?? "")}".Trim(),
                    appointment.SpecialtyId,
                    SpecialtyName = appointment.Specialty.TypeOfSpecialty,
                    Status = session.Status.ToString(),
                    SessionId = session.Id,
                    DayOfWeek = session.StartSessionDateTime.DayOfWeek.ToString(),
                    TimeSlotId = session.TimeSlotId,
                    SessionStatus = session.Status.ToString()
                });
            }
        }

        return ValueResult<(IEnumerable<object>, int)>.Success((result, totalCount));
    }

    public async Task<ValueResult<List<(DateTime Start, DateTime End)>>> GetAppointmentPreviewAsync(AppointmentDto dto)
    {
        var validationResult = _validator.Validate(dto);
        if (!validationResult.IsValid)
            return ValueResult<List<(DateTime, DateTime)>>.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

        var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
        if (patient == null)
            return ValueResult<List<(DateTime, DateTime)>>.Failure("Paciente no encontrado.");

        var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
        if (specialty == null)
            return ValueResult<List<(DateTime, DateTime)>>.Failure("Especialidad no encontrada.");

        var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
        if (specialist == null)
            return ValueResult<List<(DateTime, DateTime)>>.Failure("Especialista no encontrado.");

        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
            .ToListAsync();

        var scheduledDates = new List<(DateTime, DateTime)>();
        var currentDate = DateTime.UtcNow.Date;
        var sessionsAssigned = 0;

        var sessionsPerSlot = dto.ScheduledSessions.Count > 0
            ? (int)Math.Ceiling((double)dto.SessionCount / dto.ScheduledSessions.Count)
            : 0;

        foreach (var sessionDto in dto.ScheduledSessions)
        {
            if (!Enum.TryParse<DayOfWeek>(sessionDto.DayOfWeek, true, out var dayOfWeek))
                return ValueResult<List<(DateTime, DateTime)>>.Failure($"Día de la semana inválido: {sessionDto.DayOfWeek}.");

            var timeSlot = await _timeSlotRepository.GetAll()
                .FirstOrDefaultAsync(ts => ts.Id == sessionDto.TimeSlotId);
            if (timeSlot == null)
                return ValueResult<List<(DateTime, DateTime)>>.Failure($"Horario no encontrado: {sessionDto.TimeSlotId}.");

            if (!TimeOnly.TryParse(sessionDto.StartTime, out var startTime) ||
                !TimeOnly.TryParse(sessionDto.EndTime, out var endTime))
                return ValueResult<List<(DateTime, DateTime)>>.Failure($"Formato de hora inválido: {sessionDto.StartTime} - {sessionDto.EndTime}.");

            var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId && s.DayOfWeek == dayOfWeek);
            if (schedule == null)
                return ValueResult<List<(DateTime, DateTime)>>.Failure($"El horario no está disponible para {sessionDto.DayOfWeek}.");

            var daysUntilNext = ((int)dayOfWeek - (int)currentDate.DayOfWeek + 7) % 7;
            if (daysUntilNext == 0 && TimeOnly.FromDateTime(DateTime.UtcNow) > startTime)
                daysUntilNext = 7;

            var sessionDate = currentDate.AddDays(daysUntilNext);
            var sessionsToAssign = Math.Min(sessionsPerSlot, dto.SessionCount - sessionsAssigned);
            var maxAttempts = 100;

            for (int i = 0; i < sessionsToAssign && sessionsAssigned < dto.SessionCount && maxAttempts > 0; i++)
            {
                var startSessionDateTime = new DateTime(
                    sessionDate.Year, sessionDate.Month, sessionDate.Day,
                    startTime.Hour, startTime.Minute, 0, DateTimeKind.Utc);
                var endSessionDateTime = new DateTime(
                    sessionDate.Year, sessionDate.Month, sessionDate.Day,
                    endTime.Hour, endTime.Minute, 0, DateTimeKind.Utc);

                var isOccupied = await _scheduledSessionRepository.GetAll()
                    .AnyAsync(ss => ss.TimeSlotId == sessionDto.TimeSlotId &&
                                    ss.StartSessionDateTime == startSessionDateTime &&
                                    ss.Status != ScheduledSessionStatus.Cancelled);
                if (!isOccupied)
                {
                    scheduledDates.Add((startSessionDateTime, endSessionDateTime));
                    sessionsAssigned++;
                    sessionDate = sessionDate.AddDays(7);
                }
                else
                {
                    sessionDate = sessionDate.AddDays(7);
                    i--;
                    maxAttempts--;
                }
            }

            if (maxAttempts == 0)
                return ValueResult<List<(DateTime, DateTime)>>.Failure($"No se encontraron fechas disponibles para {sessionDto.DayOfWeek} en el horario solicitado.");
        }

        if (sessionsAssigned < dto.SessionCount)
            return ValueResult<List<(DateTime, DateTime)>>.Failure("No se pudieron programar todas las sesiones solicitadas debido a conflictos de horario.");

        return ValueResult<List<(DateTime, DateTime)>>.Success(scheduledDates);
    }
}