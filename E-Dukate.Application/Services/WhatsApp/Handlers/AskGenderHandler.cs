using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.WhatsApp.Utilities;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class AskGenderHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public AskGenderHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        if (message != "Masculino" && message != "Femenino")
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Por favor, selecciona un género válido de la lista (Masculino o Femenino).");
            await WhatsAppMessageUtils.SendGenderSelectionAsync(_whatsAppService, phoneNumber);
            return;
        }

        state.PatientData!.Gender = message == "Masculino" ? "M" : "F";
        state.Step = ConversationStep.AskAddress;
        await _whatsAppService.SendTextMessageAsync(phoneNumber, "Gracias. Ingresa tu dirección.");
    }
}