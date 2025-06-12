using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Domain.Entities.Users;
using Microsoft.Extensions.Logging;

namespace E_Dukate.Application.Services.WhatsApp.Handlers;

public class StartAppointmentHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IDialogflowService _dialogflowService;
    private readonly ILogger<StartAppointmentHandler> _logger;

    public StartAppointmentHandler(
        IWhatsAppService whatsAppService,
        IDialogflowService dialogflowService,
        ILogger<StartAppointmentHandler> logger)
    {
        _whatsAppService = whatsAppService;
        _dialogflowService = dialogflowService;
        _logger = logger;
    }

    public async Task HandleAsync(string phoneNumber, string message, ConversationState state)
    {
        _logger.LogInformation("Received message: '{Message}' for phone: '{PhoneNumber}'", message, phoneNumber);

        string response = await _dialogflowService.DetectIntentAsync(phoneNumber, message);
        _logger.LogInformation("Ollama response: '{Response}' (Length: {Length})", response, response?.Length ?? 0);

        if (response == "StartAppointment")
        {
            state.PatientData = new Patient { MobileNumber = phoneNumber };
            state.Step = ConversationStep.AskName;
            state.SelectedSpecialtyId = Guid.Empty;
            state.SelectedSpecialistId = Guid.Empty;
            state.SchedulePageIndex = 0;
            state.AvailableSchedules = new List<(string SlotId, string Title, string Description)>();

            string startMessage = "¡Hola! Vamos a reservar tu cita. Por favor, ingresa tu nombre.";
            _logger.LogInformation("Sending to WhatsApp: '{Message}'", startMessage);
            await _whatsAppService.SendTextMessageAsync(phoneNumber, startMessage);
        }
        else
        {
            string messageToSend = string.IsNullOrEmpty(response)
                ? "Lo siento, no entendí tu mensaje. ¿Quieres reservar una cita? Di 'reservar cita' para comenzar."
                : response;
            _logger.LogInformation("Sending to WhatsApp: '{Message}'", messageToSend);
            await _whatsAppService.SendTextMessageAsync(phoneNumber, messageToSend);
        }
    }
}