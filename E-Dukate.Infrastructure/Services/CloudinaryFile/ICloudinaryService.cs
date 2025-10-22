using Microsoft.AspNetCore.Http;

namespace E_Dukate.Infrastructure.Services.CloudinaryFile;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IFormFile file, string folder);
    Task<string> UploadPdfAsync(IFormFile file, string folder);
    Task<bool> DeleteFileAsync(string publicId);
    Task<string> UpdateFileAsync(IFormFile file, string publicId, string folder);
}
