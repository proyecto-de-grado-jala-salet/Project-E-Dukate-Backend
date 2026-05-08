using E_Dukate.Domain.Entities.Appointments;

namespace E_Dukate.Domain.Interfaces;

public interface ITemporaryAppointmentRepository : IGenericRepository<TemporaryAppointment>
{
    Task<List<TemporaryAppointment>> GetPendingAppointmentsAsync();
    Task<List<TemporaryAppointment>> GetApprovedAppointmentsAsync();
    Task<List<TemporaryAppointment>> GetRejectedAppointmentsAsync();
    Task<List<TemporaryAppointment>> GetByWhatsAppNumberAsync(string whatsAppNumber);
    Task CleanupExpiredAppointmentsAsync();
}