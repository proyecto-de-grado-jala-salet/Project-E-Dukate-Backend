namespace E_Dukate.Application.Interfaces.WhatsApp;

public interface IChatBotService
{
    Task ProcessMessageAsync(string phoneNumber, string message);
}
