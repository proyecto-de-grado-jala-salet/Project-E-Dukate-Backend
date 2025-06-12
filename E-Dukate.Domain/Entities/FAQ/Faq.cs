namespace E_Dukate.Domain.Entities.FAQ;

public class Faq : Primitives.Entity
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}