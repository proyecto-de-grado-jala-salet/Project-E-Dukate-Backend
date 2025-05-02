namespace E_Dukate.Application.Interfaces.WhatsApp;

public interface IWhatsAppService
{
    Task SendTextMessageAsync(string phoneNumber, string message);
    Task SendInteractiveMessageAsync(string phoneNumber, string message, string buttonTitle);
    Task SendInteractiveListMessageAsync(string phoneNumber, string message, List<(string Id, string Title)> listItems);
}
