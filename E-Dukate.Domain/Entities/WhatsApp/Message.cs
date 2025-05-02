namespace E_Dukate.Domain.Entities.WhatsApp;

public class Message
{
    public string Id { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Text Text { get; set; } = new Text();
    public InteractiveButton Interactive { get; set; } = new InteractiveButton();
}
