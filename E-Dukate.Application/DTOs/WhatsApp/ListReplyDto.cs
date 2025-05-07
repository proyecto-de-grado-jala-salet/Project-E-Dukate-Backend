using System.Text.Json.Serialization;

namespace E_Dukate.Application.DTOs.WhatsApp;

public class ListReplyDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}
