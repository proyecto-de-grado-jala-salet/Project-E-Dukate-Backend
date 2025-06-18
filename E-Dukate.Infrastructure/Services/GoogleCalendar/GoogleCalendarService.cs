using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using E_Dukate.Application.Interfaces.GoogleCalendar;
using E_Dukate.Domain.Entities.Appointments;
using Microsoft.Extensions.Configuration;

namespace E_Dukate.Infrastructure.Services.GoogleCalendar;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly string _credentialsPath;
    private readonly string _calendarId;
    private readonly CalendarService _calendarService;

    public GoogleCalendarService(IConfiguration configuration)
    {
        _credentialsPath = configuration["GoogleCalendar:CredentialsPath"]
            ?? throw new ArgumentNullException("GoogleCalendar:CredentialsPath is missing.");
        _calendarId = configuration["GoogleCalendar:CalendarId"]
            ?? throw new ArgumentNullException("GoogleCalendar:CalendarId is missing.");

        GoogleCredential credential;
        using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(CalendarService.Scope.Calendar);
        }

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "E-Dukate"
        });
    }

    public async Task<bool> CreateAppointmentEventAsync(Appointment appointment)
    {
        try
        {
            var newEvent = new Event
            {
                Summary = $"Cita: {appointment.Specialty.TypeOfSpecialty} {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal}",
                Description = $@"Cita médica con el especialista {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal} para el paciente {appointment.Patient.Names} {appointment.Patient.LastNamePaternal}.
                Cédula: {appointment.Patient.IdentityCard}
                Género: {appointment.Patient.Gender}
                Dirección: {appointment.Patient.Address}",
                Start = new EventDateTime
                {
                    // DateTime = appointment.StartTime,
                    TimeZone = "America/La_Paz"
                },
                End = new EventDateTime
                {
                    // DateTime = appointment.EndTime,
                    TimeZone = "America/La_Paz"
                }
            };

            var request = _calendarService.Events.Insert(newEvent, _calendarId);
            await request.ExecuteAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al crear evento en Google Calendar: {ex}");
            return false;
        }
    }

    public async Task<List<Event>> ListEventsAsync(Guid specialistId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var request = _calendarService.Events.List(_calendarId);
            request.TimeMinDateTimeOffset = new DateTimeOffset(startDate, TimeSpan.FromHours(-4));
            request.TimeMaxDateTimeOffset = new DateTimeOffset(endDate, TimeSpan.FromHours(-4));
            request.ShowDeleted = false;
            request.SingleEvents = true;

            var events = await request.ExecuteAsync();
            return events.Items?.ToList() ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al listar eventos de Google Calendar: {ex}");
            return new List<Event>();
        }
    }
}