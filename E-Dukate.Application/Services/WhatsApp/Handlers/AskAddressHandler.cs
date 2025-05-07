using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.Specialties;
using E_Dukate.Application.Services.WhatsApp.Utilities;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class AskAddressHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly SpecialtyService _specialtyService;

    public AskAddressHandler(IWhatsAppService whatsAppService, SpecialtyService specialtyService)
    {
        _whatsAppService = whatsAppService;
        _specialtyService = specialtyService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Por favor, ingresa una dirección válida.");
            return;
        }

        state.PatientData.Address = message.Trim();
        state.Step = ConversationStep.ShowSpecialties;

        var specialties = _specialtyService.ListAll().Select(s => (s.Id.ToString(), s.TypeOfSpecialty, s.TypeOfSpecialty)).ToList();
        if (!specialties.Any())
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "No hay especialidades disponibles.");
            state.Step = ConversationStep.None;
            return;
        }

        await _whatsAppService.SendInteractiveListMessageAsync(
            phoneNumber,
            "Selecciona una especialidad:",
            "Especialidades",
            "Elige una opción",
            "Especialidades Disponibles",
            specialties);
    }
}