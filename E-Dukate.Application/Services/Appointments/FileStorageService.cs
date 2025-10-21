using System;
using Microsoft.AspNetCore.Http;

namespace E_Dukate.Application.Services.Appointments;

public class FileStorageService
{
    private readonly string _comprobantesBasePath;

    public FileStorageService()
    {
        _comprobantesBasePath = Path.Combine(Directory.GetCurrentDirectory(), "Comprobantes");

        // Crear directorio si no existe
        if (!Directory.Exists(_comprobantesBasePath))
            Directory.CreateDirectory(_comprobantesBasePath);
    }

    public async Task<string> SaveComprobanteAsync(IFormFile file, Guid appointmentId)
    {
        try
        {
            // Validar el archivo
            if (file == null || file.Length == 0)
                throw new ArgumentException("Archivo no válido");

            // Validar tipo de archivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                throw new ArgumentException("Tipo de archivo no permitido. Use JPG, PNG, PDF o GIF.");

            // Validar tamaño (máximo 5MB)
            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("El archivo no puede ser mayor a 5MB");

            // Crear ruta de la carpeta
            var appointmentFolder = Path.Combine(_comprobantesBasePath, appointmentId.ToString());

            // Crear directorio si no existe
            if (!Directory.Exists(appointmentFolder))
                Directory.CreateDirectory(appointmentFolder);

            // Generar nombre único del archivo
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(appointmentFolder, fileName);

            // Guardar archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retornar ruta relativa para almacenar en BD
            return Path.Combine("Comprobantes", appointmentId.ToString(), fileName);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error guardando comprobante: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteComprobanteAsync(string filePath)
    {
        try
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);

                // Intentar eliminar la carpeta si está vacía
                var directory = Path.GetDirectoryName(fullPath);
                if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }

                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public string GetComprobantesBasePath()
    {
        return _comprobantesBasePath;
    }
}