using Microsoft.EntityFrameworkCore;
using E_Dukate.Domain.Entities.Payments;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Domain.Primitives;

namespace E_Dukate.Application.Services.Payments;

public class PaymentQRService
{
    private readonly IGenericRepository<PaymentQR> _paymentQRRepository;

    public PaymentQRService(IGenericRepository<PaymentQR> paymentQRRepository)
    {
        _paymentQRRepository = paymentQRRepository;
    }

    public async Task<Result> CreateQRAsync(string fileName, string fileUrl)
    {
        var existingQR = await _paymentQRRepository.GetAll().FirstOrDefaultAsync();
        if (existingQR != null)
        {
            return Result.Failure("A QR code already exists. Please update or delete the existing one.");
        }

        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(fileUrl))
        {
            return Result.Failure("Invalid file name or URL.");
        }

        try
        {
            var paymentQR = new PaymentQR
            {
                FileName = fileName,
                FilePath = fileUrl,
                UploadDate = DateTime.UtcNow
            };

            await _paymentQRRepository.AddAsync(paymentQR);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("Error creating QR code.");
        }
    }

    public async Task<ValueResult<PaymentQR>> GetQRAsync()
    {
        var qr = await _paymentQRRepository.GetAll().FirstOrDefaultAsync();
        if (qr == null)
        {
            return ValueResult<PaymentQR>.Failure("No QR code found.");
        }

        return ValueResult<PaymentQR>.Success(qr);
    }

    public async Task<Result> UpdateQRAsync(string fileName, string filePath)
    {
        var existingQR = await _paymentQRRepository.GetAll().FirstOrDefaultAsync();
        if (existingQR == null)
        {
            return Result.Failure("No QR code found to update.");
        }

        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(filePath))
        {
            return Result.Failure("Invalid file name or path.");
        }

        try
        {
            existingQR.FileName = fileName;
            existingQR.FilePath = filePath;
            existingQR.UploadDate = DateTime.UtcNow;

            await _paymentQRRepository.UpdateAsync(existingQR);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("Error updating QR code.");
        }
    }

    public async Task<Result> DeleteQRAsync()
    {
        var qr = await _paymentQRRepository.GetAll().FirstOrDefaultAsync();
        if (qr == null)
        {
            return Result.Failure("No QR code found.");
        }

        try
        {
            await _paymentQRRepository.DeleteAsync(qr.Id);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("Error deleting QR code.");
        }
    }
}