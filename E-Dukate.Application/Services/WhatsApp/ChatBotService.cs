using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.WhatsApp.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace E_Dukate.Application.Services.WhatsApp;

public class ChatBotService : IChatBotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<ConversationStep, Type> _handlerTypes;

    public ChatBotService(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        _handlerTypes = new Dictionary<ConversationStep, Type>
        {
            { ConversationStep.None, typeof(StartAppointmentHandler) },
            { ConversationStep.AskName, typeof(AskNameHandler) },
            { ConversationStep.AskLastNamePaternal, typeof(AskLastNamePaternalHandler) },
            { ConversationStep.AskIdentityCard, typeof(AskIdentityCardHandler) },
            { ConversationStep.AskDateOfBirth, typeof(AskDateOfBirthHandler) },
            { ConversationStep.AskGender, typeof(AskGenderHandler) },
            { ConversationStep.AskAddress, typeof(AskAddressHandler) },
            { ConversationStep.ShowSpecialties, typeof(ShowSpecialtiesHandler) },
            { ConversationStep.ShowSpecialists, typeof(ShowSpecialistsHandler) },
            { ConversationStep.ShowSchedules, typeof(ShowSchedulesHandler) },
            { ConversationStep.ShowMoreSchedules, typeof(ShowSchedulesHandler) }
        };
    }

    public async Task ProcessMessageAsync(string phoneNumber, string message)
    {
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IConversationStateManager>();
        var state = stateManager.GetOrCreateState(phoneNumber);

        if (_handlerTypes.TryGetValue(state.Step, out var handlerType))
        {
            var handler = (IConversationStateHandler)scope.ServiceProvider.GetRequiredService(handlerType);
            await handler.HandleAsync(phoneNumber, message, state);

            stateManager.UpdateState(phoneNumber, state);

            if (state.Step == ConversationStep.None)
            {
                stateManager.RemoveState(phoneNumber);
            }
        }
        else
        {
            throw new InvalidOperationException($"No handler found for step {state.Step}");
        }
    }
}