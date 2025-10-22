using System;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace E_Dukate.Infrastructure.Services.CloudinaryFile;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        var account = new Account(cloudName, apiKey, apiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder)
    {
        await using var stream = file.OpenReadStream();
        
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = Guid.NewGuid().ToString(),
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        
        if (uploadResult.Error != null)
            throw new Exception($"Error uploading image: {uploadResult.Error.Message}");

        return uploadResult.SecureUrl.ToString();
    }

    public async Task<string> UploadPdfAsync(IFormFile file, string folder)
    {
        await using var stream = file.OpenReadStream();
        
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder,
            PublicId = Guid.NewGuid().ToString(),
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, "raw");
        
        if (uploadResult.Error != null)
            throw new Exception($"Error uploading PDF: {uploadResult.Error.Message}");

        return uploadResult.SecureUrl.ToString();
    }

    public async Task<bool> DeleteFileAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);
        return result.Result == "ok";
    }

    public async Task<string> UpdateFileAsync(IFormFile file, string publicId, string folder)
    {
        // Primero eliminar el archivo existente
        await DeleteFileAsync(publicId);
        
        // Luego subir el nuevo
        if (file.ContentType == "application/pdf")
            return await UploadPdfAsync(file, folder);
        else
            return await UploadImageAsync(file, folder);
    }

    private string GetPublicIdFromUrl(string url)
    {
        var uri = new Uri(url);
        var segments = uri.Segments;
        var publicIdWithExtension = segments[^1];
        var publicId = Path.GetFileNameWithoutExtension(publicIdWithExtension);
        return publicId;
    }
}