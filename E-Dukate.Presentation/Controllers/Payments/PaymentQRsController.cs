using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Payments;

namespace E_Dukate.Presentation.Controllers.Payments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentQRsController : ControllerBase
{
    private readonly PaymentQRService _paymentQRService;
    private readonly IWebHostEnvironment _environment;
    private readonly string _uploadsFolder;

    public PaymentQRsController(
        PaymentQRService paymentQRService,
        IWebHostEnvironment environment)
    {
        _paymentQRService = paymentQRService;
        _environment = environment;
        _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "AppointmentPayments");
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UploadQR(IFormFile file)
    {
        try
        {

            if (file == null || file.Length == 0)
            {
                return BadRequest("No valid file provided.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Invalid file type. Only JPG, JPEG, PNG, and WebP are allowed.");
            }

            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }

            var existingFiles = Directory.GetFiles(_uploadsFolder);
            if (existingFiles.Any())
            {
                return BadRequest("A QR code already exists. Please update or delete the existing one.");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var filePath = Path.Combine(_uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var result = await _paymentQRService.CreateQRAsync(uniqueFileName, filePath);
            if (!result.IsSuccess)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
                return BadRequest(result.ErrorMessage);
            }

            return Ok(new { 
                QRId = uniqueFileName,
                Message = "QR code uploaded successfully" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error uploading QR: {ex.Message}" });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetQR()
    {
        try
        {
            var result = await _paymentQRService.GetQRAsync();
            if (!result.IsSuccess)
            {
                return NotFound(result.ErrorMessage);
            }

            var qr = result.Value!;
            var filePath = qr.FilePath;
            
            if (!System.IO.File.Exists(filePath))
            {
                await _paymentQRService.DeleteQRAsync();
                return NotFound("QR file not found on server.");
            }

            var fileExtension = Path.GetExtension(filePath).ToLower();
            var contentType = fileExtension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, contentType, qr.FileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error retrieving QR: {ex.Message}" });
        }
    }

    [HttpPut]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdateQR(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No valid file provided.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest("Invalid file type. Only JPG, JPEG, PNG, and WebP are allowed.");
            }

            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }

            var existingQRResult = await _paymentQRService.GetQRAsync();
            if (!existingQRResult.IsSuccess)
            {
                return NotFound(existingQRResult.ErrorMessage);
            }

            var existingFilePath = existingQRResult.Value!.FilePath;
            if (System.IO.File.Exists(existingFilePath))
            {
                System.IO.File.Delete(existingFilePath);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
            var newFilePath = Path.Combine(_uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(newFilePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var updateResult = await _paymentQRService.UpdateQRAsync(uniqueFileName, newFilePath);
            if (!updateResult.IsSuccess)
            {
                if (System.IO.File.Exists(newFilePath))
                {
                    System.IO.File.Delete(newFilePath);
                }
                return BadRequest(updateResult.ErrorMessage);
            }

            return Ok("QR code updated successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error updating QR: {ex.Message}" });
        }
    }

    [HttpDelete]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteQR()
    {
        try
        {
            var existingQRResult = await _paymentQRService.GetQRAsync();
            if (!existingQRResult.IsSuccess)
            {
                return NotFound(existingQRResult.ErrorMessage);
            }

            if (System.IO.File.Exists(existingQRResult.Value!.FilePath))
            {
                System.IO.File.Delete(existingQRResult.Value!.FilePath);
            }

            var result = await _paymentQRService.DeleteQRAsync();
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok("QR deleted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error deleting QR: {ex.Message}" });
        }
    }
}