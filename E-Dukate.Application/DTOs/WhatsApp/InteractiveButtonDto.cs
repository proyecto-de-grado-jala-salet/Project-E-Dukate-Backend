using System.Text.Json.Serialization;

namespace E_Dukate.Application.DTOs.WhatsApp;

public class InteractiveButtonDto
{
    [JsonPropertyName("button_reply")]
    public ButtonReplyDto ButtonReply { get; set; } = new ButtonReplyDto();

    [JsonPropertyName("list_reply")]
    public ListReplyDto ListReply { get; set; } = new ListReplyDto();
}