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
        Console.WriteLine($"Processing message: '{message}' for phone: {phoneNumber}");

        var foodItems = new List<(string Id, string Title, string Description)>
        {
            ("zapallo", "Zapallo", "Delicioso zapallo asado"),
            ("sopa", "Sopa", "Sopa casera caliente"),
            ("pollo", "Pollo", "Pollo a la parrilla")
        };

        if (!string.IsNullOrEmpty(message))
        {
            if (foodItems.Any(item => item.Title.Equals(message, StringComparison.OrdinalIgnoreCase)))
            {
                string responseMessage = $"Ok, seleccionaste {message} como mensaje de WhatsApp.";
                Console.WriteLine($"Sending confirmation: {responseMessage}");
                await _whatsAppService.SendTextMessageAsync(phoneNumber, responseMessage);
                return;
            }

            if (message.Equals("Obtener Mensaje", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Detected 'Obtener Mensaje'. Sending default message.");
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "¡Aquí tienes tu mensaje! Gracias por interactuar.");
                return;
            }
        }

        var intent = await _dialogflowService.DetectIntentAsync(phoneNumber, message);

        if (intent == "Greeting")
        {
            Console.WriteLine("Detected Greeting intent.");
            await _whatsAppService.SendInteractiveMessageAsync(
                phoneNumber,
                "¡Hola! Bienvenido(a) al chatbot de E-Dukate. ¿Cómo puedo ayudarte?",
                "greeting_button",
                "Obtener Mensaje"
            );
        }
        else if (intent == "ShowFoods" || message.ToLower() == "mostrar comidas")
        {
            Console.WriteLine("Detected ShowFoods intent or 'mostrar comidas'. Sending food list.");
            await _whatsAppService.SendInteractiveListMessageAsync(
                phoneNumber,
                "Por favor, selecciona una comida de la lista:",
                "Menú de Comidas",
                "Elige tu favorita",
                "Comidas Disponibles",
                foodItems
            );
        }
        else
        {
            Console.WriteLine("Unrecognized message or intent. Sending fallback response.");
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "No entendí tu mensaje. Intenta saludar con 'hola' o di 'mostrar comidas' para ver las opciones.");
        }
    }
}