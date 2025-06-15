namespace E_Dukate.Application.DTOs.Payments;

public class PaymentFilterDto
{
    public Guid? SpecialistId { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Day { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}