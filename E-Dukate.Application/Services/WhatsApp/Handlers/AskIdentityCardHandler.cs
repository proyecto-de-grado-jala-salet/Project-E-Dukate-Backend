using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.WhatsApp.Utilities;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class AskIdentityCardHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public AskIdentityCardHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        if (!int.TryParse(message, out int identityCard))
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Por favor, ingresa un número válido para la cédula.");
            return;
        }

        state.PatientData.IdentityCard = identityCard;
        state.Step = ConversationStep.AskDateOfBirth;
        await _whatsAppService.SendTextMessageAsync(phoneNumber, "Gracias. Ingresa tu fecha de nacimiento (formato: DD/MM/YYYY, ejemplo: 16/08/2004).");
    }
}