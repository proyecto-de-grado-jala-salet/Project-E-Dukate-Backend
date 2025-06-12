using E_Dukate.Application.Interfaces.WhatsApp;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;

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
        if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Phone number and message cannot be empty.");
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "text",
            text = new
            {
                body = message
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendInteractiveMessageAsync(string phoneNumber, string message, string buttonId, string buttonTitle)
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
                                id = buttonId,
                                title = buttonTitle
                            }
                        }
                    }
                }
            }
        };

        var response = await _httpClient.PostAsJsonAsync($"{_phoneNumberId}/messages", payload);
        if (!response.IsSuccessStatusCode)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task SendInteractiveListMessageAsync(
        string phoneNumber,
        string message,
        string header,
        string footer,
        string sectionTitle,
        List<(string Id, string Title, string Description)> listItems)
    {
        if (listItems == null || !listItems.Any())
            throw new ArgumentException("List items cannot be empty.");

        foreach (var item in listItems)
        {
            if (string.IsNullOrEmpty(item.Id) || item.Id.Length > 200)
                throw new ArgumentException($"Invalid ID: {item.Id}. Must be non-empty and less than 200 characters.");
            if (string.IsNullOrEmpty(item.Title) || item.Title.Length > 24)
                throw new ArgumentException($"Invalid Title: {item.Title}. Must be non-empty and less than 24 characters.");
            if (item.Description != null && item.Description.Length > 72)
                throw new ArgumentException($"Invalid Description: {item.Description}. Must be less than 72 characters.");
        }
        
        sectionTitle = Truncate(sectionTitle, 24);

        var payload = new
        {
            messaging_product = "whatsapp",
            to = phoneNumber,
            type = "interactive",
            interactive = new
            {
                type = "list",
                header = new { type = "text", text = header },
                body = new { text = message },
                footer = new { text = footer },
                action = new
                {
                    button = "Ver opciones",
                    sections = new[]
                    {
                        new
                        {
                            title = sectionTitle,
                            rows = listItems.Select(item => new
                            {
                                id = item.Id,
                                title = item.Title,
                                description = item.Description
                            }).ToArray()
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{_phoneNumberId}/messages", content);

        if (!response.IsSuccessStatusCode)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    private string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}