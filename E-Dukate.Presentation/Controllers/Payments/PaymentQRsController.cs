using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.Services.Payments;
using E_Dukate.Infrastructure.Services.CloudinaryFile;

namespace E_Dukate.Presentation.Controllers.Payments;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentQRsController : ControllerBase
{
    private readonly PaymentQRService _paymentQRService;
    private readonly ICloudinaryService _cloudinaryService;

    public PaymentQRsController(
        PaymentQRService paymentQRService,
        ICloudinaryService cloudinaryService)
    {
        _paymentQRService = paymentQRService;
        _cloudinaryService = cloudinaryService;
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

            var existingQRResult = await _paymentQRService.GetQRAsync();
            if (existingQRResult.IsSuccess)
            {
                return BadRequest("A QR code already exists. Please update or delete the existing one.");
            }

            var imageUrl = await _cloudinaryService.UploadImageAsync(file, "payment-qrs");

            var result = await _paymentQRService.CreateQRAsync(file.FileName, imageUrl);
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Ok(new { 
                QRId = imageUrl,
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

            if (string.IsNullOrEmpty(qr.FilePath))
            {
                return NotFound("QR file URL not found.");
            }
            
            return Redirect(qr.FilePath);
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

            var existingQRResult = await _paymentQRService.GetQRAsync();
            if (!existingQRResult.IsSuccess)
            {
                return NotFound(existingQRResult.ErrorMessage);
            }

            var existingQR = existingQRResult.Value!;
            
            var newImageUrl = await _cloudinaryService.UploadImageAsync(file, "payment-qrs");

            var updateResult = await _paymentQRService.UpdateQRAsync(file.FileName, newImageUrl);
            if (!updateResult.IsSuccess)
            {
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

            var existingQR = existingQRResult.Value!;
            
            await _cloudinaryService.DeleteFileAsync(existingQR.FilePath);

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