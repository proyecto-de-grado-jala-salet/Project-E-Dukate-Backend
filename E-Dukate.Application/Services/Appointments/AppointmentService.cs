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
using E_Dukate.Application.Interfaces.GoogleCalendar;

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
    private readonly IGoogleCalendarService _googleCalendarService;

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
        IValidator<AppointmentDto> validator,
        IGoogleCalendarService googleCalendarService)
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
        _googleCalendarService = googleCalendarService;
    }

    public async Task<Result> CreateAppointmentAsync(AppointmentDto dto)
    {
        try
        {
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
                return Result.Failure(string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            
            var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
            if (patient == null) return Result.Failure("Patient not found.");

            var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
            if (specialty == null) return Result.Failure("Specialty not found.");

            var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
            if (specialist == null) return Result.Failure("Specialist not found.");

            var schedules = await _scheduleRepository.GetAll()
                .Include(s => s.TimeSlots)
                .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
                .ToListAsync();
            
            var sessionConfigurations = await ValidateAndMapSessionConfigurationsAsync(dto, schedules);
            var scheduledDates = await GenerateScheduledDatesAsync(dto, sessionConfigurations);
            var scheduledSessions = ConvertToScheduledSessions(scheduledDates, sessionConfigurations);

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

            try
            {
                var calendarResult = await _googleCalendarService.CreateAppointmentEventAsync(appointment);
                Console.WriteLine("📅 Intentando crear evento en Google Calendar...");
                if (!calendarResult)
                {
                    Console.WriteLine("Advertencia: No se pudo crear el evento en Google Calendar, pero la cita fue guardada.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear evento en Google Calendar: {ex.Message}");
            }

            return Result.Success();
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> UpdateAppointmentAsync(Guid id, AppointmentDto dto)
    {
        try
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
            if (patient == null) return Result.Failure("Patient not found.");

            var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
            if (specialty == null) return Result.Failure("Specialty not found.");

            var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
            if (specialist == null) return Result.Failure("Specialist not found.");

            var schedules = await _scheduleRepository.GetAll()
                .Include(s => s.TimeSlots)
                .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
                .ToListAsync();
            
            var sessionConfigurations = await ValidateAndMapSessionConfigurationsAsync(dto, schedules);
            var scheduledDates = await GenerateScheduledDatesAsync(dto, sessionConfigurations, id);
            var scheduledSessions = ConvertToScheduledSessions(scheduledDates, sessionConfigurations);

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
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> ConfirmSessionAsync(Guid appointmentId, Guid sessionId)
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
            return Result.Failure("No puedes confirmar una sesión cancelada.");

        if (session.Status == ScheduledSessionStatus.Confirmed)
            return Result.Failure("La sesión ya está confirmada.");

        if (session.StartSessionDateTime <= DateTime.UtcNow)
            return Result.Failure("No se puede confirmar una sesión pasada.");

        if (appointment.Payment.AmountPaid < appointment.Payment.TotalAmount / 2)
            return Result.Failure("El monto pagado debe ser al menos la mitad del monto total para confirmar la sesión.");

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

        if (activeSessions == 0)
        {
            var deleteResult = await _paymentService.DeletePaymentAsync(appointment.Payment.Id);
            if (!deleteResult.IsSuccess)
                return Result.Failure($"Error al eliminar el pago: {deleteResult.ErrorMessage}");

            appointment.Payment = null;
            appointment.PaymentId = null;
        }
        else
        {
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
        }

        await _appointmentRepository.UpdateAsync(appointment);

        return Result.Success();
    }

    public async Task<Result> RescheduleSessionAsync(Guid appointmentId, RescheduleSessionDto dto)
    {
        try
        {
            // Obtener la cita
            var appointment = await _appointmentRepository.GetAll()
                .Include(a => a.ScheduledSessions)
                .Include(a => a.Specialist)
                .ThenInclude(s => s.Schedules)
                .ThenInclude(sch => sch.TimeSlots)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return Result.Failure("Cita no encontrada");

            // Encontrar la sesión a reprogramar
            var sessionToUpdate = appointment.ScheduledSessions
                .FirstOrDefault(s => s.Id == dto.SessionId);

            if (sessionToUpdate == null)
                return Result.Failure("Sesión no encontrada");

            // Validar que el nuevo horario esté disponible
            var isAvailable = await IsTimeSlotAvailable(
                appointment.SpecialistId,
                dto.NewStartDateTime,
                dto.NewEndDateTime,
                appointmentId);

            if (!isAvailable)
                return Result.Failure("El horario seleccionado ya no está disponible");

            // Validar que el time slot pertenezca al especialista
            var isValidTimeSlot = await _timeSlotRepository.GetAll()
                .AnyAsync(ts => ts.Id == dto.NewTimeSlotId &&
                               ts.Schedule!.SpecialistId == appointment.SpecialistId);

            if (!isValidTimeSlot)
                return Result.Failure("El horario seleccionado no es válido para este especialista");

            // Actualizar la sesión
            sessionToUpdate.TimeSlotId = dto.NewTimeSlotId;
            sessionToUpdate.StartSessionDateTime = dto.NewStartDateTime;
            sessionToUpdate.EndSessionDateTime = dto.NewEndDateTime;
            sessionToUpdate.Status = ScheduledSessionStatus.Rescheduled;

            await _scheduledSessionRepository.UpdateAsync(sessionToUpdate);

            // Actualizar en Google Calendar si es necesario
            try
            {
                await _googleCalendarService.UpdateAppointmentEventsAsync(appointment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar evento en Google Calendar: {ex.Message}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error al reprogramar la sesión: {ex.Message}");
        }
    }

    public async Task<ValueResult<List<AvailableTimeSlotDto>>> GetReschedulePreviewAsync(
    Guid appointmentId,
    ReschedulePreviewRequestDto request)
    {
        try
        {
            // Obtener la cita actual con todas las relaciones necesarias
            var appointment = await _appointmentRepository.GetAll()
                .Include(a => a.Specialist)
                .Include(a => a.ScheduledSessions)
                .Include(a => a.Specialist.Schedules)
                    .ThenInclude(s => s.TimeSlots)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
                return ValueResult<List<AvailableTimeSlotDto>>.Failure("Cita no encontrada");

            // Obtener la sesión que se quiere reprogramar
            var sessionToReschedule = appointment.ScheduledSessions
                .FirstOrDefault(s => s.Id == request.SessionId);

            if (sessionToReschedule == null)
                return ValueResult<List<AvailableTimeSlotDto>>.Failure("Sesión no encontrada");

            // Obtener el especialista y sus horarios
            var specialist = appointment.Specialist;
            var availableSlots = new List<AvailableTimeSlotDto>();

            // Obtener todas las citas existentes del especialista para verificar disponibilidad
            var existingAppointments = await _appointmentRepository.GetAll()
                .Include(a => a.ScheduledSessions)
                .Where(a => a.SpecialistId == specialist.Id && a.Id != appointmentId)
                .ToListAsync();

            // Buscar horarios disponibles para el día seleccionado (esta semana y siguiente)
            if (!string.IsNullOrEmpty(request.TargetDayOfWeek))
            {
                var startDate = DateTime.UtcNow.Date;
                var targetDay = Enum.Parse<DayOfWeek>(request.TargetDayOfWeek);

                for (int weekOffset = 0; weekOffset < request.LookAheadWeeks; weekOffset++)
                {
                    var targetDate = GetNextWeekday(startDate, targetDay);
                    targetDate = targetDate.AddDays(7 * weekOffset);

                    // Excluir la fecha y hora actual de la sesión
                    if (targetDate.Date == sessionToReschedule.StartSessionDateTime.Date)
                    {
                        // Si es el mismo día, excluir el horario actual
                        var slotsForDate = await GetAvailableSlotsForDateExcludingCurrent(
                            specialist,
                            targetDate,
                            existingAppointments,
                            sessionToReschedule,
                            appointmentId);
                        availableSlots.AddRange(slotsForDate);
                    }
                    else
                    {
                        // Para otros días, buscar todos los horarios disponibles
                        var slotsForDate = await GetAvailableSlotsForDate(
                            specialist,
                            targetDate,
                            existingAppointments,
                            sessionToReschedule.StartSessionDateTime,
                            appointmentId);
                        availableSlots.AddRange(slotsForDate);
                    }
                }
            }

            // Eliminar duplicados y ordenar
            availableSlots = availableSlots
                .GroupBy(slot => new { slot.StartDateTime, slot.EndDateTime })
                .Select(group => group.First())
                .OrderBy(slot => slot.StartDateTime)
                .ToList();

            return ValueResult<List<AvailableTimeSlotDto>>.Success(availableSlots);
        }
        catch (Exception ex)
        {
            return ValueResult<List<AvailableTimeSlotDto>>.Failure($"Error al obtener horarios disponibles: {ex.Message}");
        }
    }

    private async Task<List<AvailableTimeSlotDto>> GetAvailableSlotsForDateExcludingCurrent(
    Specialist specialist,
    DateTime date,
    List<Appointment> existingAppointments,
    ScheduledSession currentSession,
    Guid currentAppointmentId)
    {
        var availableSlots = new List<AvailableTimeSlotDto>();

        // Obtener el horario del especialista para este día de la semana
        var daySchedule = specialist.Schedules
            .FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek && s.Attends);

        if (daySchedule == null)
            return availableSlots;

        foreach (var timeSlot in daySchedule.TimeSlots.OrderBy(ts => ts.StartTime))
        {
            var slotStartDateTime = date.Add(timeSlot.StartTime.ToTimeSpan());
            var slotEndDateTime = date.Add(timeSlot.EndTime.ToTimeSpan());

            // Verificar si el slot está en el pasado
            if (slotStartDateTime <= DateTime.UtcNow)
                continue;

            // Excluir el horario actual de la sesión
            if (slotStartDateTime.TimeOfDay == currentSession.StartSessionDateTime.TimeOfDay &&
                slotEndDateTime.TimeOfDay == currentSession.EndSessionDateTime.TimeOfDay)
            {
                continue; // Saltar el horario actual
            }

            // Verificar si el horario está ocupado
            var isOccupied = existingAppointments
                .SelectMany(a => a.ScheduledSessions)
                .Any(session => session.StartSessionDateTime < slotEndDateTime &&
                               session.EndSessionDateTime > slotStartDateTime &&
                               session.Status != ScheduledSessionStatus.Cancelled);

            if (!isOccupied)
            {
                availableSlots.Add(new AvailableTimeSlotDto
                {
                    TimeSlotId = timeSlot.Id,
                    StartDateTime = slotStartDateTime,
                    EndDateTime = slotEndDateTime,
                    DayOfWeek = date.DayOfWeek.ToString(),
                    FormattedDate = date.ToString("dd/MM/yyyy"),
                    FormattedTime = $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}",
                    IsSameDay = date.Date == currentSession.StartSessionDateTime.Date,
                    IsNextWeek = date.Date > currentSession.StartSessionDateTime.Date.AddDays(7)
                });
            }
        }

        return availableSlots;
    }

    private DateTime GetNextWeekday(DateTime start, DayOfWeek day)
    {
        int daysToAdd = ((int)day - (int)start.DayOfWeek + 7) % 7;
        return start.AddDays(daysToAdd == 0 ? 7 : daysToAdd); // Si es el mismo día, buscar la siguiente semana
    }

    private async Task<List<AvailableTimeSlotDto>> GetAvailableSlotsForDate(
    Specialist specialist,
    DateTime date,
    List<Appointment> existingAppointments,
    DateTime originalSessionTime,
    Guid currentAppointmentId)
    {
        var availableSlots = new List<AvailableTimeSlotDto>();

        // Obtener el horario del especialista para este día de la semana
        var daySchedule = specialist.Schedules
            .FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek && s.Attends);

        if (daySchedule == null)
            return availableSlots;

        foreach (var timeSlot in daySchedule.TimeSlots.OrderBy(ts => ts.StartTime))
        {
            var slotStartDateTime = date.Add(timeSlot.StartTime.ToTimeSpan());
            var slotEndDateTime = date.Add(timeSlot.EndTime.ToTimeSpan());

            // Verificar si el slot está en el pasado
            if (slotStartDateTime <= DateTime.UtcNow)
                continue;

            // Verificar si el horario está ocupado
            var isOccupied = existingAppointments
                .SelectMany(a => a.ScheduledSessions)
                .Any(session => session.StartSessionDateTime < slotEndDateTime &&
                               session.EndSessionDateTime > slotStartDateTime &&
                               session.Status != ScheduledSessionStatus.Cancelled);

            if (!isOccupied)
            {
                availableSlots.Add(new AvailableTimeSlotDto
                {
                    TimeSlotId = timeSlot.Id,
                    StartDateTime = slotStartDateTime,
                    EndDateTime = slotEndDateTime,
                    DayOfWeek = date.DayOfWeek.ToString(),
                    FormattedDate = date.ToString("dd/MM/yyyy"),
                    FormattedTime = $"{timeSlot.StartTime:hh\\:mm} - {timeSlot.EndTime:hh\\:mm}",
                    IsSameDay = date.Date == originalSessionTime.Date,
                    IsNextWeek = date.Date > originalSessionTime.Date.AddDays(7)
                });
            }
        }

        return availableSlots;
    }

    private async Task<bool> IsTimeSlotAvailable(Guid specialistId, DateTime start, DateTime end, Guid excludeAppointmentId)
    {
        var conflictingSessions = await _scheduledSessionRepository.GetAll()
            .Include(ss => ss.Appointment)
            .Where(ss => ss.Appointment!.SpecialistId == specialistId &&
                        ss.AppointmentId != excludeAppointmentId &&
                        ss.Status != ScheduledSessionStatus.Cancelled &&
                        ss.StartSessionDateTime < end &&
                        ss.EndSessionDateTime > start)
            .ToListAsync();

        return !conflictingSessions.Any();
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
        try
        {
            var validationResult = _validator.Validate(dto);
            if (!validationResult.IsValid)
                return ValueResult<List<(DateTime Start, DateTime End)>>.Failure(
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            
            var patient = await _patientRepository.GetByIdAsync(dto.PatientId);
            if (patient == null) return ValueResult<List<(DateTime Start, DateTime End)>>.Failure("Paciente no encontrado.");

            var specialty = await _specialtyRepository.GetByIdAsync(dto.SpecialtyId);
            if (specialty == null) return ValueResult<List<(DateTime Start, DateTime End)>>.Failure("Especialidad no encontrada.");

            var specialist = await _specialistRepository.GetByIdAsync(dto.SpecialistId);
            if (specialist == null) return ValueResult<List<(DateTime Start, DateTime End)>>.Failure("Especialista no encontrado.");

            var schedules = await _scheduleRepository.GetAll()
                .Include(s => s.TimeSlots)
                .Where(s => s.SpecialistId == dto.SpecialistId && s.Attends)
                .ToListAsync();
            
            var sessionConfigurations = await ValidateAndMapSessionConfigurationsAsync(dto, schedules);
            var scheduledDates = await GenerateScheduledDatesAsync(dto, sessionConfigurations);

            return ValueResult<List<(DateTime Start, DateTime End)>>.Success(scheduledDates);
        }
        catch (ArgumentException ex)
        {
            return ValueResult<List<(DateTime Start, DateTime End)>>.Failure(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ValueResult<List<(DateTime Start, DateTime End)>>.Failure(ex.Message);
        }
    }

    private async Task<List<(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, Guid TimeSlotId, string Status)>>
        ValidateAndMapSessionConfigurationsAsync(AppointmentDto dto, List<Schedule> schedules)
    {
        var sessionConfigurations = new List<(DayOfWeek, TimeOnly, TimeOnly, Guid, string)>();

        foreach (var sessionDto in dto.ScheduledSessions)
        {
            if (!Enum.TryParse<DayOfWeek>(sessionDto.DayOfWeek, true, out var dayOfWeek))
                throw new ArgumentException($"Día de la semana inválido: {sessionDto.DayOfWeek}.");

            var timeSlot = await _timeSlotRepository.GetAll()
                .FirstOrDefaultAsync(ts => ts.Id == sessionDto.TimeSlotId);
            if (timeSlot == null)
                throw new ArgumentException($"Horario no encontrado: {sessionDto.TimeSlotId}.");

            if (!TimeOnly.TryParse(sessionDto.StartTime, out var startTime) ||
                !TimeOnly.TryParse(sessionDto.EndTime, out var endTime))
                throw new ArgumentException($"Formato de hora inválido: {sessionDto.StartTime} - {sessionDto.EndTime}.");

            var schedule = schedules.FirstOrDefault(s => s.Id == timeSlot.ScheduleId && s.DayOfWeek == dayOfWeek);
            if (schedule == null)
                throw new ArgumentException($"El horario no está disponible para {sessionDto.DayOfWeek}.");

            sessionConfigurations.Add((dayOfWeek, startTime, endTime, sessionDto.TimeSlotId, sessionDto.Status));
        }

        if (!sessionConfigurations.Any())
            throw new ArgumentException("No se han seleccionado días para las sesiones.");

        return sessionConfigurations;
    }

    private async Task<List<(DateTime Start, DateTime End)>> GenerateScheduledDatesAsync(
    AppointmentDto dto,
    List<(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, Guid TimeSlotId, string Status)> sessionConfigurations,
    Guid? excludeAppointmentId = null)
    {
        var scheduledDates = new List<(DateTime Start, DateTime End)>();
        var currentDate = DateTime.UtcNow.Date;

        for (int sessionNumber = 0; sessionNumber < dto.SessionCount; sessionNumber++)
        {
            var sessionConfig = sessionConfigurations[sessionNumber % sessionConfigurations.Count];

            var sessionDate = await FindNextAvailableDateAsync(
                sessionConfig, currentDate, sessionNumber, sessionConfigurations.Count,
                excludeAppointmentId, dto.PatientId, dto.SpecialtyId, scheduledDates);

            scheduledDates.Add((sessionDate.Start, sessionDate.End));
        }

        return scheduledDates.OrderBy(x => x.Start).ToList();
    }

    private async Task<(DateTime Start, DateTime End)> FindNextAvailableDateAsync(
        (DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, Guid TimeSlotId, string Status) sessionConfig,
        DateTime currentDate,
        int sessionNumber,
        int totalConfigurations,
        Guid? excludeAppointmentId,
        Guid patientId,
        Guid specialtyId,
        List<(DateTime Start, DateTime End)> existingDates)
    {
        var weeksToAdd = sessionNumber / totalConfigurations;
        var attempt = 0;
        const int maxAttempts = 100;

        while (attempt < maxAttempts)
        {
            var daysUntilDay = ((int)sessionConfig.DayOfWeek - (int)currentDate.DayOfWeek + 7) % 7;
            var baseDate = currentDate.AddDays(daysUntilDay);

            if (daysUntilDay == 0 && TimeOnly.FromDateTime(DateTime.UtcNow) > sessionConfig.StartTime)
            {
                baseDate = baseDate.AddDays(7);
            }

            var targetDate = baseDate.AddDays((weeksToAdd + attempt) * 7);

            var startSessionDateTime = new DateTime(
                targetDate.Year, targetDate.Month, targetDate.Day,
                sessionConfig.StartTime.Hour, sessionConfig.StartTime.Minute, 0, DateTimeKind.Utc);
            var endSessionDateTime = new DateTime(
                targetDate.Year, targetDate.Month, targetDate.Day,
                sessionConfig.EndTime.Hour, sessionConfig.EndTime.Minute, 0, DateTimeKind.Utc);
            
            var isValid = await IsValidSessionDateAsync(
                sessionConfig.TimeSlotId, startSessionDateTime, excludeAppointmentId, patientId, specialtyId, existingDates);

            if (isValid)
            {
                return (startSessionDateTime, endSessionDateTime);
            }

            attempt++;
        }

        throw new InvalidOperationException($"No se pudo encontrar una fecha disponible para la sesión después de {maxAttempts} intentos.");
    }

    private async Task<bool> IsValidSessionDateAsync(
    Guid timeSlotId,
    DateTime startSessionDateTime,
    Guid? excludeAppointmentId,
    Guid patientId,
    Guid specialtyId,
    List<(DateTime Start, DateTime End)> existingDates)
    {
        if (existingDates.Any(ed => ed.Start == startSessionDateTime))
            return false;
        
        var isTimeSlotOccupied = await _scheduledSessionRepository.GetAll()
            .AnyAsync(ss => ss.TimeSlotId == timeSlotId &&
                            ss.StartSessionDateTime == startSessionDateTime &&
                            ss.Status != ScheduledSessionStatus.Cancelled &&
                            (excludeAppointmentId == null || ss.AppointmentId != excludeAppointmentId));

        if (isTimeSlotOccupied)
            return false;
        
        var hasAnyAppointmentSameTime = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .AnyAsync(a => a.PatientId == patientId &&
                          (excludeAppointmentId == null || a.Id != excludeAppointmentId) &&
                          a.ScheduledSessions.Any(ss =>
                              ss.StartSessionDateTime == startSessionDateTime &&
                              ss.Status != ScheduledSessionStatus.Cancelled));

        if (hasAnyAppointmentSameTime)
            return false;
        
        var hasSameSpecialtySameDay = await _appointmentRepository.GetAll()
            .Include(a => a.ScheduledSessions)
            .AnyAsync(a => a.PatientId == patientId &&
                          a.SpecialtyId == specialtyId &&
                          (excludeAppointmentId == null || a.Id != excludeAppointmentId) &&
                          a.ScheduledSessions.Any(ss =>
                              ss.StartSessionDateTime.Date == startSessionDateTime.Date &&
                              ss.Status != ScheduledSessionStatus.Cancelled));

        if (hasSameSpecialtySameDay)
            return false;
        
        var hasSameSpecialtySameDayInPreview = existingDates.Any(ed =>
            ed.Start.Date == startSessionDateTime.Date);

        if (hasSameSpecialtySameDayInPreview)
            return false;

        return true;
    }

    private List<ScheduledSession> ConvertToScheduledSessions(
    List<(DateTime Start, DateTime End)> scheduledDates,
    List<(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, Guid TimeSlotId, string Status)> sessionConfigurations)
    {
        var scheduledSessions = new List<ScheduledSession>();

        for (int i = 0; i < scheduledDates.Count; i++)
        {
            var sessionConfig = sessionConfigurations[i % sessionConfigurations.Count];
            var date = scheduledDates[i];

            scheduledSessions.Add(new ScheduledSession
            {
                TimeSlotId = sessionConfig.TimeSlotId,
                StartSessionDateTime = date.Start,
                EndSessionDateTime = date.End,
                Status = Enum.Parse<ScheduledSessionStatus>(sessionConfig.Status, true)
            });
        }

        return scheduledSessions;
    }
}