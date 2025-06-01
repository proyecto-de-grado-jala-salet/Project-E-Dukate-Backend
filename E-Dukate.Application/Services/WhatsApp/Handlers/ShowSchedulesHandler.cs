using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.Specialties;
using E_Dukate.Application.Services.Users;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Application.Interfaces.GoogleCalendar;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Application.Utilities;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class ShowSchedulesHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly SpecialtyService _specialtyService;
    private readonly SpecialistService _specialistService;
    private readonly PatientService _patientService;
    private readonly IGoogleCalendarService _googleCalendarService;

    public ShowSchedulesHandler(
        IWhatsAppService whatsAppService,
        SpecialtyService specialtyService,
        SpecialistService specialistService,
        PatientService patientService,
        IGoogleCalendarService googleCalendarService)
    {
        _whatsAppService = whatsAppService;
        _specialtyService = specialtyService;
        _specialistService = specialistService;
        _patientService = patientService;
        _googleCalendarService = googleCalendarService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        if (message == "Ver más horarios")
        {
            state.SchedulePageIndex++;
            state.Step = ConversationStep.ShowMoreSchedules;
            await ShowSchedulePageAsync(phoneNumber, state);
            return;
        }

        var scheduleParts = message.Split(' ');
        if (scheduleParts.Length < 2)
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Horario no válido. Por favor, selecciona uno de la lista.");
            return;
        }

        var day = scheduleParts[0];
        var timeRange = scheduleParts[1].Split('-');
        if (!Enum.TryParse<DayOfWeek>(day, true, out var dayOfWeek) || timeRange.Length != 2)
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Formato de horario no válido. Por favor, selecciona uno de la lista.");
            return;
        }

        if (!TimeOnly.TryParse(timeRange[0], out var startTime) || !TimeOnly.TryParse(timeRange[1], out var endTime))
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Hora no válida. Por favor, selecciona uno de la lista.");
            return;
        }

        var patientDto = new PatientDto
        {
            Names = state.PatientData!.Names,
            LastNamePaternal = state.PatientData.LastNamePaternal,
            LastNameMaternal = state.PatientData.LastNameMaternal ?? "",
            MobileNumber = PhoneNumberUtils.NormalizePhoneNumber(state.PatientData.MobileNumber),
            IdentityCard = state.PatientData.IdentityCard,
            PhoneNumber = state.PatientData.PhoneNumber,
            Age = state.PatientData.Age,
            Gender = state.PatientData.Gender,
            DateOfBirth = state.PatientData.DateOfBirth,
            Address = state.PatientData.Address
        };

        var existingPatient = _patientService.ListAll().FirstOrDefault(p =>
            p.IdentityCard == patientDto.IdentityCard &&
            p.Names == patientDto.Names &&
            p.LastNamePaternal == patientDto.LastNamePaternal);

        Patient patient;
        if (existingPatient == null)
        {
            var patientResult = _patientService.Register(patientDto);
            if (!patientResult.IsSuccess)
            {
                await _whatsAppService.SendTextMessageAsync(phoneNumber, $"Error al registrar paciente: {patientResult.ErrorMessage}");
                state.Step = ConversationStep.None;
                return;
            }

            patient = _patientService.ListAll().FirstOrDefault(p =>
                p.IdentityCard == patientDto.IdentityCard &&
                p.Names == patientDto.Names &&
                p.LastNamePaternal == patientDto.LastNamePaternal)?? 
                throw new InvalidOperationException("Paciente no encontrado después de registro");

            if (patient == null)
            {
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "Error al recuperar datos del paciente.");
                state.Step = ConversationStep.None;
                return;
            }
        }
        else
        {
            patient = existingPatient;
        }

        var appointmentDate = GetNextDateForDayOfWeek(dayOfWeek);
        var appointmentStartTime = new DateTime(
            appointmentDate.Year,
            appointmentDate.Month,
            appointmentDate.Day,
            startTime.Hour,
            startTime.Minute,
            0
        );
        var appointmentEndTime = appointmentStartTime.AddMinutes(45);

        var specialist = _specialistService.GetSpecialistById(state.SelectedSpecialistId)?? 
                     throw new InvalidOperationException("Especialista no encontrado");
        var specialty = _specialtyService.FindById(state.SelectedSpecialtyId)?? 
                    throw new InvalidOperationException("Especialidad no encontrada");

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            Patient = patient,
            SpecialistId = specialist.Id,
            Specialist = specialist,
            SpecialtyId = specialty.Id,
            Specialty = specialty,
            StartTime = appointmentStartTime,
            EndTime = appointmentEndTime
        };

        var eventCreated = await _googleCalendarService.CreateAppointmentEventAsync(appointment);
        if (!eventCreated)
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Error al crear la cita en Google Calendar. Intenta de nuevo.");
            state.Step = ConversationStep.None;
            return;
        }

        var confirmationMessage = $@"Tu cita ha sido agendada con éxito:
            - Especialidad: {specialty.TypeOfSpecialty}
            - Especialista: {specialist.Names} {specialist.LastNamePaternal}
            - Fecha y hora: {appointmentStartTime:dd/MM/yyyy HH:mm}
            - Duración: 45 minutos
            - Paciente: {patient.Names} {patient.LastNamePaternal}
            - Cédula: {patient.IdentityCard}
            - Fecha de nacimiento: {patient.DateOfBirth:dd/MM/yyyy}
            - Género: {patient.Gender}
            - Dirección: {patient.Address}
            ¡Gracias por usar E-Dukate!";

        await _whatsAppService.SendTextMessageAsync(phoneNumber, confirmationMessage);

        state.Step = ConversationStep.None;
    }

    private async Task ShowSchedulePageAsync(string phoneNumber, ConversationState state)
    {
        const int pageSize = 9;
        var schedules = state.AvailableSchedules
            .Skip(state.SchedulePageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        if (state.AvailableSchedules.Count > (state.SchedulePageIndex + 1) * pageSize)
        {
            schedules.Add(("more_schedules", "Ver más horarios", ""));
        }

        if (!schedules.Any())
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "No hay más horarios disponibles para este especialista.");
            state.Step = ConversationStep.None;
            return;
        }

        await _whatsAppService.SendInteractiveListMessageAsync(
            phoneNumber,
            "Selecciona un horario:",
            "Horarios",
            $"Página {state.SchedulePageIndex + 1}",
            "Horarios Disponibles",
            schedules);
    }

    private DateTime GetNextDateForDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.Today;
        var daysUntilNext = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNext == 0) daysUntilNext = 7;
        return today.AddDays(daysUntilNext);
    }
}