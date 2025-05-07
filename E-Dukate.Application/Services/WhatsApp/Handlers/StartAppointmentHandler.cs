using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Services.WhatsApp.Utilities;
using E_Dukate.Domain.Entities.Users;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class StartAppointmentHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IDialogflowService _dialogflowService;

    public StartAppointmentHandler(
        IWhatsAppService whatsAppService,
        IDialogflowService dialogflowService)
    {
        _whatsAppService = whatsAppService;
        _dialogflowService = dialogflowService;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        string intent = await _dialogflowService.DetectIntentAsync(phoneNumber, message);

        var normalizedMessage = message.ToLower().Trim();
        if (intent == "StartAppointment" || 
            normalizedMessage == "reservar cita" || 
            normalizedMessage.Contains("realizar una cita") || 
            normalizedMessage.Contains("agendar cita"))
        {
            state.PatientData = new Patient { MobileNumber = phoneNumber };
            state.Step = ConversationStep.AskName;
            state.SelectedSpecialtyId = Guid.Empty;
            state.SelectedSpecialistId = Guid.Empty;
            state.SchedulePageIndex = 0;
            state.AvailableSchedules = new List<(string SlotId, string Title, string Description)>();

            await _whatsAppService.SendTextMessageAsync(phoneNumber, "¡Hola! Vamos a reservar tu cita. Por favor, ingresa tu nombre.");
        }
        else
        {
            await WhatsAppMessageUtils.SendErrorMessageAsync(_whatsAppService, phoneNumber, "No entendí tu mensaje. Di 'reservar cita' para comenzar.");
        }
    }
}