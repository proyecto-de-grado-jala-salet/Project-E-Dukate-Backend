using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.WhatsApp.Utilities;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class AskLastNamePaternalHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public AskLastNamePaternalHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Por favor, ingresa un apellido válido.");
            return;
        }

        state.PatientData!.LastNamePaternal = message.Trim();
        state.Step = ConversationStep.AskIdentityCard;
        await _whatsAppService.SendTextMessageAsync(phoneNumber, "Perfecto. Ingresa tu número de cédula de identidad (CI).");
    }
}