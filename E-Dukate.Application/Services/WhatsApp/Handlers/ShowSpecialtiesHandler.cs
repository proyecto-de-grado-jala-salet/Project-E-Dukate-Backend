using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.Specialties;
using E_Dukate.Application.Services.Users;
using E_Dukate.Application.Services.WhatsApp.Utilities;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class ShowSpecialtiesHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly SpecialtyService _specialtyService;
    private readonly SpecialistService _specialistService;

    public ShowSpecialtiesHandler(IWhatsAppService whatsAppService, SpecialtyService specialtyService, SpecialistService specialistService)
    {
        _whatsAppService = whatsAppService;
        _specialtyService = specialtyService;
        _specialistService = specialistService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        var selectedSpecialty = _specialtyService.ListAll().FirstOrDefault(s => s.TypeOfSpecialty == message);
        if (selectedSpecialty == null)
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "Especialidad no válida. Por favor, selecciona una de la lista.");
            return;
        }

        state.SelectedSpecialtyId = selectedSpecialty.Id;
        state.Step = ConversationStep.ShowSpecialists;

        var specialists = _specialistService.GetAllSpecialists()
            .Where(s => s.Specialty.Id == state.SelectedSpecialtyId)
            .Select(s => (s.Id.ToString(), $"{s.Names} {s.LastNamePaternal}", s.SpecialistCode))
            .ToList();

        if (!specialists.Any())
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "No hay especialistas disponibles para esta especialidad.");
            state.Step = ConversationStep.None;
            return;
        }

        await _whatsAppService.SendInteractiveListMessageAsync(
            phoneNumber,
            "Selecciona un especialista:",
            "Especialistas",
            "Elige una opción",
            "Especialistas Disponibles",
            specialists);
    }
}