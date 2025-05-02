using E_Dukate.Application.Interfaces.WhatsApp;

namespace E_Dukate.Application.Services.WhatsApp;

public class ChatBotService : IChatBotService
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IDialogflowService _dialogflowService;

    public ChatBotService(IWhatsAppService whatsAppService, IDialogflowService dialogflowService)
    {
        _whatsAppService = whatsAppService;
        _dialogflowService = dialogflowService;
    }

    public async Task ProcessMessageAsync(string phoneNumber, string message)
    {
        var foodItems = new List<(string Id, string Title)>
        {
            ("zapallo", "Zapallo"),
            ("sopa", "Sopa"),
            ("pollo", "Pollo")
        };

        Console.WriteLine($"Processing message: '{message}' for phone: {phoneNumber}");

        if (foodItems.Any(item => item.Title.Equals(message, StringComparison.OrdinalIgnoreCase)))
        {
            string responseMessage = $"Ok, seleccionaste {message} como mensaje de WhatsApp.";
            Console.WriteLine($"Sending confirmation: {responseMessage}");
            await _whatsAppService.SendTextMessageAsync(phoneNumber, responseMessage);
            return;
        }
        
        var intent = await _dialogflowService.DetectIntentAsync(phoneNumber, message);

        if (intent == "Greeting")
        {
            Console.WriteLine("Detected Greeting intent.");
            await _whatsAppService.SendInteractiveMessageAsync(
                phoneNumber,
                "¡Hola! Bienvenido(a) al chatbot de E-Dukate. ¿Cómo puedo ayudarte?",
                "Obtener Mensaje"
            );
        }
        else if (intent == "ShowFoods" || message.ToLower() == "mostrar comidas")
        {
            Console.WriteLine("Detected ShowFoods intent or 'mostrar comidas'. Sending food list.");
            await _whatsAppService.SendInteractiveListMessageAsync(
                phoneNumber,
                "Por favor, selecciona una comida de la lista:",
                foodItems
            );
        }
        else if (message == "Obtener Mensaje")
        {
            Console.WriteLine("Detected 'Obtener Mensaje'. Sending default message.");
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "¡Aquí tienes tu mensaje! Gracias por interactuar.");
        }
        else
        {
            Console.WriteLine("Unrecognized message or intent. Sending fallback response.");
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "No entendí tu mensaje. Intenta saludar con 'hola' o di 'mostrar comidas' para ver las opciones.");
        }
    }
}