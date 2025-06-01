using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.WhatsApp.Utilities;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class AskNameHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public AskNameHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Por favor, ingresa un nombre válido.");
            return;
        }

        state.PatientData!.Names = message.Trim();
        state.Step = ConversationStep.AskLastNamePaternal;
        await _whatsAppService.SendTextMessageAsync(phoneNumber, "Gracias. Ahora ingresa tu apellido paterno.");
    }
}