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
            if (appointment.ScheduledSessions == null || !appointment.ScheduledSessions.Any())
            {
                Console.WriteLine("No hay sesiones programadas para esta cita.");
                return false;
            }

            int eventColor = GetColorByPatientId(appointment.PatientId);
            var gender = appointment.Patient.Gender?.ToUpper() == "F" ? "Femenino" :
                        appointment.Patient.Gender?.ToUpper() == "M" ? "Masculino" :
                        appointment.Patient.Gender ?? "No especificado";

            bool allEventsCreated = true;

            foreach (var session in appointment.ScheduledSessions)
            {
                try
                {
                    DateTime startDateTime = session.StartSessionDateTime.AddHours(+4);
                    DateTime endDateTime = session.EndSessionDateTime.AddHours(+4);

                    var newEvent = new Event
                    {
                        Summary = $"Cita: {appointment.Specialty.TypeOfSpecialty} {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal}",
                        Description = $@"Cita médica con el especialista {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal} para el paciente {appointment.Patient.Names} {appointment.Patient.LastNamePaternal}.
                        Cédula: {appointment.Patient.IdentityCard}
                        Género: {gender}
                        Edad: {appointment.Patient.Age}",
                        Start = new EventDateTime
                        {
                            DateTime = startDateTime
                        },
                        End = new EventDateTime
                        {
                            DateTime = endDateTime
                        },
                        ColorId = eventColor.ToString(),
                    };

                    var request = _calendarService.Events.Insert(newEvent, _calendarId);
                    await request.ExecuteAsync();

                    Console.WriteLine($"✅ Evento creado en Google Calendar para sesión: {session.StartSessionDateTime}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al crear evento para sesión {session.StartSessionDateTime}: {ex.Message}");
                    allEventsCreated = false;
                }
            }

            return allEventsCreated;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error general al crear eventos en Google Calendar: {ex}");
            return false;
        }
    }

    [Obsolete]
    public async Task<List<Event>> ListEventsAsync(Guid specialistId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var request = _calendarService.Events.List(_calendarId);

            request.TimeMin = startDate;
            request.TimeMax = endDate;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

            var events = await request.ExecuteAsync();
            return events.Items?.ToList() ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al listar eventos de Google Calendar: {ex}");
            return new List<Event>();
        }
    }

    [Obsolete]
    public async Task<bool> UpdateAppointmentEventsAsync(Appointment appointment)
    {
        try
        {
            if (appointment == null)
            {
                Console.WriteLine("❌ Appointment es null");
                return false;
            }

            if (appointment.ScheduledSessions == null || !appointment.ScheduledSessions.Any())
            {
                Console.WriteLine("❌ No hay sesiones programadas para esta cita.");
                return false;
            }

            // VALIDAR PROPIEDADES REQUERIDAS
            if (appointment.Patient == null)
            {
                Console.WriteLine("❌ Patient es null");
                return false;
            }

            if (appointment.Specialist == null)
            {
                Console.WriteLine("❌ Specialist es null");
                return false;
            }

            if (appointment.Specialty == null)
            {
                Console.WriteLine("❌ Specialty es null");
                return false;
            }

            var startDate = appointment.ScheduledSessions.Min(s => s.StartSessionDateTime).AddMonths(-1);
            var endDate = appointment.ScheduledSessions.Max(s => s.StartSessionDateTime).AddMonths(1);

            var existingEvents = await ListEventsAsync(appointment.SpecialistId, startDate, endDate);

            if (existingEvents == null)
            {
                Console.WriteLine("⚠️ No se pudieron obtener los eventos existentes, creando lista vacía.");
                existingEvents = new List<Event>();
            }

            var gender = appointment.Patient.Gender?.ToUpper() == "F" ? "Femenino" :
                        appointment.Patient.Gender?.ToUpper() == "M" ? "Masculino" :
                        appointment.Patient.Gender ?? "No especificado";

            bool allEventsUpdated = true;

            foreach (var session in appointment.ScheduledSessions)
            {
                try
                {
                    Console.WriteLine($"🔄 Procesando sesión: {session.StartSessionDateTime}");

                    DateTime startDateTime = session.StartSessionDateTime.AddHours(+4);
                    DateTime endDateTime = session.EndSessionDateTime.AddHours(+4);
                    string identityCard = appointment.Patient.IdentityCard.ToString() ?? "Sin cédula";
                    
                    var existingEvent = existingEvents.FirstOrDefault(e =>
                        e.Description?.Contains(identityCard) == true);

                    var eventSummary = $"Cita: {appointment.Specialty.TypeOfSpecialty} - {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal}";
                    var eventDescription = $@"Cita médica con el especialista {appointment.Specialist.Names} {appointment.Specialist.LastNamePaternal} para el paciente {appointment.Patient.Names} {appointment.Patient.LastNamePaternal}.
                Cédula: {identityCard}
                Género: {gender}
                Edad: {appointment.Patient.Age}
                Estado: {session.Status}";

                    if (existingEvent != null)
                    {
                        try
                        {
                            var deleteRequest = _calendarService.Events.Delete(_calendarId, existingEvent.Id);
                            await deleteRequest.ExecuteAsync();
                            Console.WriteLine($"✅ Evento anterior eliminado: {existingEvent.Id}");
                        }
                        catch (Exception deleteEx)
                        {
                            Console.WriteLine($"⚠️ No se pudo eliminar el evento anterior: {deleteEx.Message}");
                        }
                    }

                    var newEvent = new Event
                    {
                        Summary = eventSummary,
                        Description = eventDescription,
                        Start = new EventDateTime { DateTime = startDateTime },
                        End = new EventDateTime { DateTime = endDateTime },
                        ColorId = GetColorByPatientId(appointment.PatientId).ToString(),
                    };

                    var insertRequest = _calendarService.Events.Insert(newEvent, _calendarId);
                    var createdEvent = await insertRequest.ExecuteAsync();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error al procesar sesión {session.StartSessionDateTime}: {ex.Message}");
                    Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                    allEventsUpdated = false;
                }
            }

            return allEventsUpdated;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error general al actualizar eventos en Google Calendar: {ex.Message}");
            Console.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            return false;
        }
    }

    [Obsolete]
    public async Task<List<Event>> GetEventsByPatientIdentityAsync(string identityCard, DateTime startDate, DateTime endDate)
    {
        try
        {
            var allEvents = await ListEventsAsync(Guid.Empty, startDate, endDate);
            return allEvents.Where(e => e.Description?.Contains(identityCard) == true).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al buscar eventos por cédula: {ex.Message}");
            return new List<Event>();
        }
    }

    [Obsolete]
    private async Task DeleteAppointmentEventsAsync(Appointment appointment)
    {
        try
        {
            // Buscar eventos existentes por el título/descripción
            var startDate = appointment.ScheduledSessions.Min(s => s.StartSessionDateTime).AddMonths(-1);
            var endDate = appointment.ScheduledSessions.Max(s => s.StartSessionDateTime).AddMonths(1);

            var events = await ListEventsAsync(appointment.SpecialistId, startDate, endDate);

            var appointmentEvents = events.Where(e =>
                e.Description?.Contains((char)appointment.Patient.IdentityCard) == true ||
                e.Summary?.Contains(appointment.Specialty.TypeOfSpecialty) == true).ToList();

            foreach (var eventItem in appointmentEvents)
            {
                try
                {
                    var deleteRequest = _calendarService.Events.Delete(_calendarId, eventItem.Id);
                    await deleteRequest.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al eliminar evento {eventItem.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al buscar eventos para eliminar: {ex.Message}");
        }
    }

    [Obsolete]
    public async Task<bool> DeleteAppointmentEventAsync(Appointment appointment)
    {
        try
        {
            await DeleteAppointmentEventsAsync(appointment);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al eliminar evento de Google Calendar: {ex.Message}");
            return false;
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