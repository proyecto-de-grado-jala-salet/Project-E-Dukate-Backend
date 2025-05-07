namespace E_Dukate.Application.Interfaces.WhatsApp;

public interface IWhatsAppService
{
    Task SendTextMessageAsync(string phoneNumber, string message);
    Task SendInteractiveMessageAsync(string phoneNumber, string message, string buttonId, string buttonTitle);
    Task SendInteractiveListMessageAsync(
        string phoneNumber,
        string message,
        string header,
        string footer,
        string sectionTitle,
        List<(string Id, string Title, string Description)> listItems);
}
