using E_Dukate.Application.Interfaces.GoogleCalendar;
using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.Users;
using E_Dukate.Application.Services.WhatsApp.Utilities;
using E_Dukate.Domain.Entities.Schedules;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class ShowSpecialistsHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly SpecialistService _specialistService;
    private readonly ScheduleService _scheduleService;
    private readonly IGoogleCalendarService _googleCalendarService;

    public ShowSpecialistsHandler(IWhatsAppService whatsAppService, SpecialistService specialistService, ScheduleService scheduleService, IGoogleCalendarService googleCalendarService)
    {
        _whatsAppService = whatsAppService;
        _specialistService = specialistService;
        _scheduleService = scheduleService;
        _googleCalendarService = googleCalendarService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        var selectedSpecialist = _specialistService.GetAllSpecialists().FirstOrDefault(s => $"{s.Names} {s.LastNamePaternal}" == message);
        if (selectedSpecialist == null)
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Especialista no válido. Por favor, selecciona uno de la lista.");
            return;
        }

        state.SelectedSpecialistId = selectedSpecialist.Id;
        state.SchedulePageIndex = 0;
        state.AvailableSchedules = _scheduleService.GetSchedulesBySpecialistId(selectedSpecialist.Id)
            .Where(s => s.Attends)
            .SelectMany(s => GenerateAvailableSlots(s, selectedSpecialist.Id))
            .Select(slot => (slot.SlotId, $"{slot.Day} {slot.StartTime:HH:mm}-{slot.EndTime:HH:mm}", ""))
            .ToList();
        state.Step = ConversationStep.ShowSchedules;

        await ShowSchedulePageAsync(phoneNumber, state, _whatsAppService);
    }

    private async Task ShowSchedulePageAsync(string phoneNumber, ConversationState state, IWhatsAppService whatsAppService)
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
            await WhatsAppMessageUtils.SendErrorMessageAsync(whatsAppService, phoneNumber, "No hay más horarios disponibles para este especialista.");
            state.Step = ConversationStep.None;
            return;
        }

        await whatsAppService.SendInteractiveListMessageAsync(
            phoneNumber,
            "Selecciona un horario:",
            "Horarios",
            $"Página {state.SchedulePageIndex + 1}",
            "Horarios Disponibles",
            schedules);
    }

    private List<(string SlotId, string Day, TimeOnly StartTime, TimeOnly EndTime)> GenerateAvailableSlots(Schedule schedule, Guid specialistId)
    {
        var slots = new List<(string SlotId, string Day, TimeOnly StartTime, TimeOnly EndTime)>();
        var day = schedule.DayOfWeek.ToString();
        var appointmentDate = GetNextDateForDayOfWeek(schedule.DayOfWeek);

        var occupiedSlots = GetOccupiedSlots(specialistId, appointmentDate).GetAwaiter().GetResult();

        foreach (var timeSlot in schedule.TimeSlots)
        {
            var currentTime = timeSlot.StartTime;
            while (currentTime < timeSlot.EndTime)
            {
                var slotEndTime = currentTime.Add(TimeSpan.FromMinutes(45));
                if (slotEndTime > timeSlot.EndTime) break;
                
                var slotStartDateTime = new DateTime(
                    appointmentDate.Year,
                    appointmentDate.Month,
                    appointmentDate.Day,
                    currentTime.Hour,
                    currentTime.Minute,
                    0
                );
                var slotEndDateTime = slotStartDateTime.AddMinutes(45);

                if (!occupiedSlots.Any(slot => 
                    (slot.Start <= slotStartDateTime && slot.End > slotStartDateTime) ||
                    (slot.Start < slotEndDateTime && slot.End >= slotEndDateTime) ||
                    (slot.Start >= slotStartDateTime && slot.End <= slotEndDateTime)))
                {
                    var slotId = $"{day}_{currentTime:HH\\:mm}";
                    slots.Add((slotId, day, currentTime, slotEndTime));
                }

                currentTime = slotEndTime;
            }
        }

        return slots;
    }

    private async Task<List<(DateTime Start, DateTime End)>> GetOccupiedSlots(Guid specialistId, DateTime date)
    {
        var occupiedSlots = new List<(DateTime Start, DateTime End)>();
        try
        {
            var events = await _googleCalendarService.ListEventsAsync(specialistId, date, date.AddDays(1));
            foreach (var evt in events)
            {
                if (evt.Start.DateTime.HasValue && evt.End.DateTime.HasValue)
                {
                    occupiedSlots.Add((evt.Start.DateTime.Value, evt.End.DateTime.Value));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al obtener eventos de Google Calendar: {ex}");
        }
        return occupiedSlots;
    }

    private DateTime GetNextDateForDayOfWeek(DayOfWeek dayOfWeek)
    {
        var today = DateTime.Today;
        var daysUntilNext = ((int)dayOfWeek - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNext == 0) daysUntilNext = 7;
        return today.AddDays(daysUntilNext);
    }
}