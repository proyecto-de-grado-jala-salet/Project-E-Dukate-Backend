using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Domain.Entities.Appointments;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using System.Text.Json;

namespace E_Dukate.Application.Services.Appointments;

public class TemporaryAppointmentService
{
    private readonly ITemporaryAppointmentRepository _temporaryAppointmentRepository;
    private readonly FileStorageService _fileStorageService;

    public TemporaryAppointmentService(
        ITemporaryAppointmentRepository temporaryAppointmentRepository,
        FileStorageService fileStorageService)
    {
        _temporaryAppointmentRepository = temporaryAppointmentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<Guid> CreateTemporaryAppointmentAsync(CreateTemporaryAppointmentRequestDto request)
    {
        var temporaryAppointment = new TemporaryAppointment
        {
            WhatsAppNumber = request.WhatsAppNumber,
            AppointmentData = JsonSerializer.Serialize(request.AppointmentData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsProcessed = false,
            Status = "Pending"
        };

        await _temporaryAppointmentRepository.AddAsync(temporaryAppointment);
        return temporaryAppointment.Id;
    }

    public async Task<TemporaryAppointmentResponseDto?> GetTemporaryAppointmentAsync(Guid id)
    {
        var appointment = await _temporaryAppointmentRepository.GetByIdAsync(id);
        if (appointment == null)
            return null;

        return new TemporaryAppointmentResponseDto
        {
            Id = appointment.Id,
            WhatsAppNumber = appointment.WhatsAppNumber,
            AppointmentData = JsonSerializer.Deserialize<object>(appointment.AppointmentData) ?? new(),
            ComprobanteUrl = appointment.ComprobanteUrl,
            ComprobanteFileName = appointment.ComprobanteFileName,
            CreatedAt = appointment.CreatedAt,
            ExpiresAt = appointment.ExpiresAt,
            IsProcessed = appointment.IsProcessed,
            Status = appointment.Status,
            PaymentUploadedAt = appointment.PaymentUploadedAt,
            ProcessedAt = appointment.ProcessedAt
        };
    }

    public async Task<List<TemporaryAppointmentResponseDto>> GetPendingAppointmentsAsync()
    {
        var appointments = await _temporaryAppointmentRepository.GetPendingAppointmentsAsync();

        return appointments.Select(appointment => new TemporaryAppointmentResponseDto
        {
            Id = appointment.Id,
            WhatsAppNumber = appointment.WhatsAppNumber,
            AppointmentData = JsonSerializer.Deserialize<object>(appointment.AppointmentData) ?? new(),
            ComprobanteUrl = appointment.ComprobanteUrl,
            ComprobanteFileName = appointment.ComprobanteFileName,
            CreatedAt = appointment.CreatedAt,
            ExpiresAt = appointment.ExpiresAt,
            IsProcessed = appointment.IsProcessed,
            Status = appointment.Status,
            PaymentUploadedAt = appointment.PaymentUploadedAt,
            ProcessedAt = appointment.ProcessedAt
        }).ToList();
    }

    public async Task<Result> UploadComprobanteAsync(UploadComprobanteRequestDto request)
    {
        try
        {
            var appointment = await _temporaryAppointmentRepository.GetByIdAsync(request.TemporaryAppointmentId);
            if (appointment == null)
                return Result.Failure("Cita temporal no encontrada");

            if (appointment.IsProcessed)
                return Result.Failure("Esta cita ya ha sido procesada");

            if (appointment.ExpiresAt < DateTime.UtcNow)
                return Result.Failure("Esta cita ha expirado");

            var filePath = await _fileStorageService.SaveComprobanteAsync(request.Comprobante, appointment.Id);
            
            appointment.ComprobanteUrl = filePath;
            appointment.ComprobanteFileName = request.Comprobante.FileName;
            appointment.Status = "Payment_Uploaded";
            appointment.PaymentUploadedAt = DateTime.UtcNow;

            await _temporaryAppointmentRepository.UpdateAsync(appointment);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Error subiendo comprobante: {ex.Message}");
        }
    }

    public async Task<Result> VerifyAppointmentAsync(Guid id, VerifyTemporaryAppointmentRequestDto request)
    {
        var appointment = await _temporaryAppointmentRepository.GetByIdAsync(id);
        if (appointment == null)
            return Result.Failure("Cita temporal no encontrada");

        if (appointment.IsProcessed)
            return Result.Failure("Esta cita ya ha sido procesada");

        appointment.IsProcessed = true;
        appointment.Status = request.IsApproved ? "Approved" : "Rejected";
        appointment.ProcessedAt = DateTime.UtcNow;

        await _temporaryAppointmentRepository.UpdateAsync(appointment);

        return Result.Success();
    }

    public async Task CleanupExpiredAppointmentsAsync()
    {
        await _temporaryAppointmentRepository.CleanupExpiredAppointmentsAsync();
    }
}
