using E_Dukate.Application.DTOs.Appointments;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;
using E_Dukate.Infrastructure.Services.CloudinaryFile;
using System.Text.Json;


namespace E_Dukate.Infrastructure.Services.TemporaryAppointment;

public class TemporaryAppointmentService
{
    private readonly ITemporaryAppointmentRepository _temporaryAppointmentRepository;
    private readonly ICloudinaryService _cloudinaryService;

    public TemporaryAppointmentService(
        ITemporaryAppointmentRepository temporaryAppointmentRepository,
        ICloudinaryService cloudinaryService)
    {
        _temporaryAppointmentRepository = temporaryAppointmentRepository;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<Guid> CreateTemporaryAppointmentAsync(CreateTemporaryAppointmentRequestDto request)
    {
        var temporaryAppointment = new Domain.Entities.Appointments.TemporaryAppointment
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
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<TemporaryAppointmentResponseDto>> GetApprovedAppointmentsAsync()
    {
        var appointments = await _temporaryAppointmentRepository.GetApprovedAppointmentsAsync();
        return appointments.Select(MapToDto).ToList();
    }

    public async Task<List<TemporaryAppointmentResponseDto>> GetRejectedAppointmentsAsync()
    {
        var appointments = await _temporaryAppointmentRepository.GetRejectedAppointmentsAsync();
        return appointments.Select(MapToDto).ToList();
    }
    
    private TemporaryAppointmentResponseDto MapToDto(Domain.Entities.Appointments.TemporaryAppointment appointment)
    {
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

    public async Task<Result> UploadComprobanteAsync(UploadComprobanteRequestDto request)
    {
        try
        {
            var appointment = await _temporaryAppointmentRepository.GetByIdAsync(request.TemporaryAppointmentId);
            if (appointment == null)
                return Result.Failure("Cita temporal no encontrada");

            if (appointment.IsProcessed)
                return Result.Failure("La cita ya ha sido procesada");

            // Validar archivo
            if (request.Comprobante == null || request.Comprobante.Length == 0)
                return Result.Failure("Archivo no válido");

            // Validar tipo de archivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".gif" };
            var fileExtension = Path.GetExtension(request.Comprobante.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return Result.Failure("Tipo de archivo no permitido. Use JPG, PNG, PDF o GIF.");

            // Validar tamaño (máximo 5MB)
            if (request.Comprobante.Length > 5 * 1024 * 1024)
                return Result.Failure("El archivo no puede ser mayor a 5MB");

            // Subir a Cloudinary
            string comprobanteUrl;
            if (fileExtension == ".pdf")
            {
                comprobanteUrl = await _cloudinaryService.UploadPdfAsync(request.Comprobante, "comprobantes");
            }
            else
            {
                comprobanteUrl = await _cloudinaryService.UploadImageAsync(request.Comprobante, "comprobantes");
            }

            // Actualizar la cita temporal
            appointment.ComprobanteUrl = comprobanteUrl;
            appointment.ComprobanteFileName = request.Comprobante.FileName;
            appointment.PaymentUploadedAt = DateTime.UtcNow;
            appointment.Status = "Payment_Uploaded";

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
