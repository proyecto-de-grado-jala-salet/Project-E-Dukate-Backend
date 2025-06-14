using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.WhatsApp.Utilities;
using E_Dukate.Application.Utilities;
using System.Globalization;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class AskDateOfBirthHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public AskDateOfBirthHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        if (!DateOnly.TryParseExact(message, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dob))
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Por favor, ingresa una fecha válida en el formato DD/MM/YYYY (ejemplo: 15/07/2004).");
            return;
        }

        state.PatientData!.DateOfBirth = dob;
        state.PatientData.Age = DateTimeUtils.CalculateAge(dob);
        state.Step = ConversationStep.AskGender;
        await WhatsAppMessageUtils.SendGenderSelectionAsync(_whatsAppService, phoneNumber);
    }
}