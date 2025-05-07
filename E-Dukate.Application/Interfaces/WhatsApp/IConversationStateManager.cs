using E_Dukate.Application.Services.WhatsApp;

namespace E_Dukate.Application.Interfaces.WhatsApp;

public interface IConversationStateManager
{
    ConversationState GetOrCreateState(string phoneNumber);
    void UpdateState(string phoneNumber, ConversationState state);
    void RemoveState(string phoneNumber);
}