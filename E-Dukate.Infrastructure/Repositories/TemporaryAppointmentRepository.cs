using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Infrastructure.Repositories;

public class TemporaryAppointmentRepository : GenericRepository<TemporaryAppointment>, ITemporaryAppointmentRepository
{
    private readonly AppDbContext _context;

    public TemporaryAppointmentRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<TemporaryAppointment>> GetPendingAppointmentsAsync()
    {
        return await _context.Set<TemporaryAppointment>()
            .Where(ta => ta.Status == "Payment_Uploaded" && !ta.IsProcessed)
            .OrderByDescending(ta => ta.PaymentUploadedAt)
            .ToListAsync();
    }

    public async Task<List<TemporaryAppointment>> GetApprovedAppointmentsAsync()
    {
        return await _context.Set<TemporaryAppointment>()
            .Where(ta => ta.Status == "Approved" && ta.IsProcessed)
            .OrderByDescending(ta => ta.ProcessedAt)
            .ToListAsync();
    }
    
    public async Task<List<TemporaryAppointment>> GetRejectedAppointmentsAsync()
    {
        return await _context.Set<TemporaryAppointment>()
            .Where(ta => ta.Status == "Rejected" && ta.IsProcessed)
            .OrderByDescending(ta => ta.ProcessedAt)
            .ToListAsync();
    }

    public async Task<List<TemporaryAppointment>> GetByWhatsAppNumberAsync(string whatsAppNumber)
    {
        return await _context.Set<TemporaryAppointment>()
            .Where(ta => ta.WhatsAppNumber == whatsAppNumber)
            .OrderByDescending(ta => ta.CreatedAt)
            .ToListAsync();
    }

    public async Task CleanupExpiredAppointmentsAsync()
    {
        var expiredAppointments = await _context.Set<TemporaryAppointment>()
            .Where(ta => ta.ExpiresAt < DateTime.UtcNow && !ta.IsProcessed)
            .ToListAsync();

        _context.Set<TemporaryAppointment>().RemoveRange(expiredAppointments);
        await _context.SaveChangesAsync();
    }
}