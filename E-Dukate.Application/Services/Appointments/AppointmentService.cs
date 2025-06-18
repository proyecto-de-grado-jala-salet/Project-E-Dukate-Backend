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

    public async Task<Result> DeleteAppointmentAsync(Guid id)
    {
        var appointment = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment == null)
            return Result.Failure("Appointment not found.");

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

    public async Task<Result> CancelSessionAsync(Guid sessionId, Guid patientId)
    {
        var session = await _scheduledSessionRepository.GetAll()
            .Include(ss => ss.Appointment)
            .ThenInclude(a => a!.Patient)
            .FirstOrDefaultAsync(ss => ss.Id == sessionId);
        if (session == null)
            return Result.Failure("Session not found.");

        if (session.Appointment!.PatientId != patientId)
            return Result.Failure("You do not have permission to cancel this session.");

        if (session.Status == ScheduledSessionStatus.Cancelled)
            return Result.Failure("The session is already cancelled.");

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
            .ThenInclude(a => a!.Specialist)
            .FirstOrDefaultAsync(ss => ss.Id == sessionId);
        if (session == null)
            return Result.Failure("Session not found.");

        if (session.Appointment!.PatientId != patientId)
            return Result.Failure("You do not have permission to reschedule this session.");

        if (!Enum.TryParse<DayOfWeek>(dto.DayOfWeek, true, out var dayOfWeek))
            return Result.Failure($"Invalid day of the week: {dto.DayOfWeek}.");

        if (!TimeOnly.TryParse(dto.StartTime, out var startTime) ||
            !TimeOnly.TryParse(dto.EndTime, out var endTime))
            return Result.Failure($"Invalid time format: {dto.StartTime} - {dto.EndTime}.");

        var timeSlot = await _timeSlotRepository.GetAll()
            .FirstOrDefaultAsync(ts => ts.Id == dto.TimeSlotId &&
                                       ts.StartTime == startTime &&
                                       ts.EndTime == endTime);
        if (timeSlot == null)
            return Result.Failure($"Schedule not found: {dto.StartTime} - {dto.EndTime}.");

        var schedules = await _scheduleRepository.GetAll()
            .Include(s => s.TimeSlots)
            .Where(s => s.SpecialistId == session.Appointment.SpecialistId && s.Attends)
            .ToListAsync();

        var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId && s.DayOfWeek == dayOfWeek);
        if (schedule == null)
            return Result.Failure($"The schedule is not available for {dto.DayOfWeek}.");

        var currentDate = DateTime.UtcNow.Date;
        var daysUntilNext = ((int)dayOfWeek - (int)currentDate.DayOfWeek + 7) % 7;
        if (daysUntilNext == 0 && TimeOnly.FromDateTime(DateTime.UtcNow) > startTime)
            daysUntilNext = 7;

        var sessionDate = currentDate.AddDays(daysUntilNext);
        var startSessionDateTime = sessionDate.Add(startTime.ToTimeSpan());
        var endSessionDateTime = sessionDate.Add(endTime.ToTimeSpan());

        var isOccupied = await _scheduledSessionRepository.GetAll()
            .AnyAsync(ss => ss.TimeSlotId == dto.TimeSlotId &&
                            ss.StartSessionDateTime.Date == sessionDate &&
                            ss.StartSessionDateTime.Hour == startTime.Hour &&
                            ss.StartSessionDateTime.Minute == startTime.Minute &&
                            ss.Id != sessionId);
        if (isOccupied)
            return Result.Failure($"The schedule {startSessionDateTime} is already occupied.");

        session.TimeSlotId = dto.TimeSlotId;
        session.StartSessionDateTime = startSessionDateTime;
        session.EndSessionDateTime = endSessionDateTime;
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
                ss.StartSessionDateTime,
                ss.EndSessionDateTime,
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
                ss.StartSessionDateTime,
                ss.EndSessionDateTime,
                Status = ss.Status.ToString()
            }),
            a.PaymentId,
            Status = GetAppointmentStatus(a)
        });

        return (result, totalCount);
    }
}