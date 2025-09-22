using E_Dukate.Domain.Primitives;

namespace E_Dukate.Domain.Entities.Payments;

public class PaymentQR : Entity
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}