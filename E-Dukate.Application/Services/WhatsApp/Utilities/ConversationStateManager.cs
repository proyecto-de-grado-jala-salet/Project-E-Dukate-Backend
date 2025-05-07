using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Utilities;
using E_Dukate.Domain.Entities.Users;
using Microsoft.Extensions.Logging;

namespace E_Dukate.Application.Services.WhatsApp.Utilities;

public class ConversationStateManager : IConversationStateManager
{
    private readonly Dictionary<string, ConversationState> _conversationStates = new();
    private readonly ILogger<ConversationStateManager> _logger;

    public ConversationStateManager(ILogger<ConversationStateManager> logger)
    {
        _logger = logger;
    }

    public ConversationState GetOrCreateState(string phoneNumber)
    {
        var normalizedPhone = PhoneNumberUtils.NormalizePhoneNumber(phoneNumber);

        if (!_conversationStates.TryGetValue(normalizedPhone, out var state))
        {
            state = new ConversationState
            {
                PatientData = new Patient { MobileNumber = normalizedPhone }
            };
            _conversationStates[normalizedPhone] = state;
        }
        else
        {
            _logger.LogInformation("Retrieved existing state for phone: step: {Step}", state.Step);
        }

        return state;
    }

    public void UpdateState(string phoneNumber, ConversationState state)
    {
        var normalizedPhone = PhoneNumberUtils.NormalizePhoneNumber(phoneNumber);
        _conversationStates[normalizedPhone] = state;
    }

    public void RemoveState(string phoneNumber)
    {
        var normalizedPhone = PhoneNumberUtils.NormalizePhoneNumber(phoneNumber);
        _logger.LogInformation("Removing state for phone: {PhoneNumber}", normalizedPhone);
        _conversationStates.Remove(normalizedPhone);
    }
}