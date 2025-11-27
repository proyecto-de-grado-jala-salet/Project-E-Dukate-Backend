using E_Dukate.Domain.Entities.Appointments;
using Google.Apis.Calendar.v3.Data;

namespace E_Dukate.Application.Interfaces.GoogleCalendar;

public interface IGoogleCalendarService
{
    Task<bool> CreateAppointmentEventAsync(Appointment appointment);
    Task<bool> UpdateAppointmentEventsAsync(Appointment appointment);
    Task<List<Event>> ListEventsAsync(Guid specialistId, DateTime startDate, DateTime endDate);
    Task<List<Event>> GetEventsByPatientIdentityAsync(string identityCard, DateTime startDate, DateTime endDate);
    Task<bool> DeleteAppointmentEventAsync(Appointment appointment);
}