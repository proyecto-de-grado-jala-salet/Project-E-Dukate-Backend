using E_Dukate.Application.Interfaces.WhatsApp;

namespace E_Dukate.Application.Services.WhatsApp.Utilities;

public static class WhatsAppMessageUtils
{
    public static async Task SendErrorMessageAsync(IWhatsAppService whatsAppService, string phoneNumber, string errorMessage)
    {
        await whatsAppService.SendTextMessageAsync(phoneNumber, errorMessage);
    }

    public static async Task SendGenderSelectionAsync(IWhatsAppService whatsAppService, string phoneNumber)
    {
        var genders = new List<(string Id, string Title, string Description)>
        {
            ("Masculino", "Masculino", ""),
            ("Femenino", "Femenino", "")
        };
        await whatsAppService.SendInteractiveListMessageAsync(
            phoneNumber,
            "Selecciona tu género:",
            "Géneros",
            "Elige una opción",
            "Géneros Disponibles",
            genders);
    }
}