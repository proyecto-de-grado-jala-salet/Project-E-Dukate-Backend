using System;
using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.Interfaces.TemporaryAppointment;

public interface ITemporaryAppointmentService
{
    Task<Guid> CreateTemporaryAppointmentAsync(CreateTemporaryAppointmentRequestDto request);
    Task<TemporaryAppointmentResponseDto?> GetTemporaryAppointmentAsync(Guid id);
    Task<List<TemporaryAppointmentResponseDto>> GetPendingAppointmentsAsync();
    Task<Result> UploadComprobanteAsync(UploadComprobanteRequestDto request);
    Task<Result> VerifyAppointmentAsync(Guid id, VerifyTemporaryAppointmentRequestDto request);
    Task CleanupExpiredAppointmentsAsync();
}
