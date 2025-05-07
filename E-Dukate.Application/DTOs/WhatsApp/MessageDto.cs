namespace E_Dukate.Application.DTOs.WhatsApp;

public class MessageDto
{
    public string Id { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public TextDto Text { get; set; } = new TextDto();
    public InteractiveButtonDto Interactive { get; set; } = new InteractiveButtonDto();
}
