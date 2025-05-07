namespace E_Dukate.Application.DTOs.WhatsApp;

public class WebhookResponseDto
{
    public EntryDto[] Entry { get; set; } = Array.Empty<EntryDto>();
}
