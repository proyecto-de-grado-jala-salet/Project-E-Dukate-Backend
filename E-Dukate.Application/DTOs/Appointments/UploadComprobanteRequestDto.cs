using Microsoft.AspNetCore.Http;

namespace E_Dukate.Application.DTOs.Appointments;

public class UploadComprobanteRequestDto
{
    public Guid TemporaryAppointmentId { get; set; }
    public IFormFile Comprobante { get; set; } = null!;
}
