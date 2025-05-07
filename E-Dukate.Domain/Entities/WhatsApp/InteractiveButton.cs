namespace E_Dukate.Domain.Entities.WhatsApp;

public class InteractiveButton
{
    public ButtonReply ButtonReply { get; set; } = new ButtonReply();
    public ListReply ListReply { get; set; } = new ListReply();
}
