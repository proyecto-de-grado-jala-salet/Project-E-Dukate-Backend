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
    private readonly string _calendarId;
    private readonly CalendarService _calendarService;

    public GoogleCalendarService(IConfiguration configuration)
    {
        _calendarId = configuration["GoogleCalendar:CalendarId"]
            ?? throw new ArgumentNullException("GoogleCalendar:CalendarId is missing.");

        GoogleCredential credential;
        
        string? credentialsJson = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS_JSON");
        Console.WriteLine("🔧 Inicializando GoogleCalendarService...");
        Console.WriteLine($"📅 CalendarId configurado: {_calendarId}");
        Console.WriteLine($"AAAAAAAAAa credentialsJson: {credentialsJson}");
        if (!string.IsNullOrEmpty(credentialsJson))
        {
            credential = GoogleCredential.FromJson(credentialsJson)
                .CreateScoped(CalendarService.Scope.Calendar);
        }
        else
        {
            var credentialsPath = configuration["GoogleCalendar:CredentialsPath"];
            if (string.IsNullOrEmpty(credentialsPath))
            {
                throw new InvalidOperationException(
                    "No se encontraron credenciales de Google Calendar. " +
                    "Configure la variable de entorno GOOGLE_APPLICATION_CREDENTIALS_JSON " +
                    "o la configuración GoogleCalendar:CredentialsPath.");
            }
            
            // Verificar que el archivo existe
            if (!File.Exists(credentialsPath))
            {
                throw new FileNotFoundException(
                    $"No se encontró el archivo de credenciales en: {credentialsPath}");
            }
            
            using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(CalendarService.Scope.Calendar);
            }
        }

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "E-Dukate"
        });
    }

    [Obsolete]
    public async Task<bool> CreateAppointmentEventAsync(Appointment appointment)
    {
        try
        {
            var firstSession = appointment.ScheduledSessions?.FirstOrDefault();
            if (firstSession == null)
            {
                Console.WriteLine("No hay sesiones programadas para esta cita.");
                return false;
            }

            int eventColor = GetColorByPatientId(appointment.PatientId);

            var boliviaOffset = TimeSpan.FromHours(-4);
        

            var startTimeBolivia = firstSession.StartSessionDateTime + boliviaOffset;
            var endTimeBolivia = firstSession.EndSessionDateTime + boliviaOffset;

            var gender = appointment.Patient.Gender?.ToUpper() == "F" ? "Femenino" : 
                    appointment.Patient.Gender?.ToUpper() == "M" ? "Masculino" : 
                    appointment.Patient.Gender ?? "No especificado";

            var newEvent = new Event
            {
                Summary = $"Cita: {appointment.Specialty.TypeOfSpecialty} {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal}",
                Description = $@"Cita médica con el especialista {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal} para el paciente {appointment.Patient.Names} {appointment.Patient.LastNamePaternal}.
                Cédula: {appointment.Patient.IdentityCard}
                Género: {gender}
                Edad: {appointment.Patient.Age}",
                Start = new EventDateTime
                {
                    DateTime = startTimeBolivia,
                    TimeZone = "America/La_Paz"
                },
                End = new EventDateTime
                {
                    DateTime = endTimeBolivia,
                    TimeZone = "America/La_Paz"
                },
                ColorId = eventColor.ToString(),
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

    private int GetColorByPatientId(Guid patientId)
    {
        byte[] patientBytes = patientId.ToByteArray();
        int seed = BitConverter.ToInt32(patientBytes, 0);
        var random = new Random(seed);

        int[] availableColors = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
        return availableColors[random.Next(availableColors.Length)];
    }
}