using E_Dukate.Application.Services.WhatsApp;

namespace E_Dukate.Application.Interfaces.WhatsApp;

public interface IConversationStateHandler
{
    Task HandleAsync(string phoneNumber, string message, ConversationState state);
}