using Microsoft.AspNetCore.Http;

namespace E_Dukate.Application.Interfaces.TemporaryAppointment;

public interface IFileStorageService
{
    Task<string> SaveComprobanteAsync(IFormFile file, Guid appointmentId);
    Task<bool> DeleteComprobanteAsync(string filePath);
    string GetComprobantesBasePath();
}
