using E_Dukate.Application.Interfaces.WhatsApp;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace E_Dukate.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string _phoneNumberId;

    public WhatsAppService(IConfiguration configuration, HttpClient httpClient)
    {
        _token = configuration["WhatsApp:Token"] ?? throw new ArgumentNullException("WhatsApp:Token is missing.");
        _phoneNumberId = configuration["WhatsApp:PhoneNumberId"] ?? throw new ArgumentNullException("WhatsApp:PhoneNumberId is missing.");
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
    }

    public async Task SendTextMessageAsync(string phoneNumber, string message)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "text",
            text = new { body = message }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_phoneNumberId}/messages", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendInteractiveMessageAsync(string phoneNumber, string message, string buttonText)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "interactive",
            interactive = new
            {
                type = "button",
                body = new { text = message },
                action = new
                {
                    buttons = new[]
                    {
                    new
                    {
                        type = "reply",
                        reply = new
                        {
                            id = "btn_obtener_mensaje",
                            title = buttonText
                        }
                    }
                }
                }
            }
        };

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        var response = await _httpClient.PostAsJsonAsync($"{_phoneNumberId}/messages", payload);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendInteractiveListMessageAsync(string phoneNumber, string message, List<(string Id, string Title)> listItems)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "interactive",
            interactive = new
            {
                type = "list",
                header = new { type = "text", text = "Menú de Comidas" },
                body = new { text = message },
                footer = new { text = "Selecciona una opción" },
                action = new
                {
                    button = "Ver opciones",
                    sections = new[]
                    {
                        new
                        {
                            title = "Comidas disponibles",
                            rows = listItems.Select(item => new
                            {
                                id = item.Id,
                                title = item.Title,
                                description = $"Selecciona {item.Title}"
                            }).ToArray()
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_phoneNumberId}/messages", payload);
        response.EnsureSuccessStatusCode();
    }
}