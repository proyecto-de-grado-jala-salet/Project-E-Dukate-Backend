using E_Dukate.Application.Interfaces.WhatsApp;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace E_Dukate.Infrastructure.Services.Ollama;

public class OllamaService : IDialogflowService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;
    private readonly string _modelName;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(IConfiguration configuration, HttpClient httpClient, ILogger<OllamaService> logger)
    {
        _apiEndpoint = configuration["Ollama:ApiEndpoint"] ?? throw new ArgumentNullException("Ollama:ApiEndpoint is missing.");
        _modelName = configuration["Ollama:ModelName"] ?? throw new ArgumentNullException("Ollama:ModelName is missing.");
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_apiEndpoint);
        _logger = logger;
    }

    public async Task<string> DetectIntentAsync(string sessionId, string message)
    {
        var prompt = $@"Eres un chatbot amigable y eficiente para E-Dukate, un sistema de reservas de citas médicas. Tu tarea principal es detectar la intención del usuario a partir de su mensaje y responder de manera adecuada, clara y profesional. La intención principal es 'StartAppointment', que se activa cuando el usuario expresa el deseo de reservar una cita médica (por ejemplo, frases como ""quiero agendar una cita"", ""necesito un turno con el doctor"", ""cómo reservo una consulta""). Si detectas la intención 'StartAppointment', responde únicamente con la palabra 'StartAppointment'.

        Si el mensaje del usuario no corresponde a 'StartAppointment', proporciona una respuesta útil, informativa o amistosa que se alinee con el contexto del mensaje. Por ejemplo:

        - Si el usuario pregunta por horarios, especialidades o información general, ofrece una respuesta clara y relevante.
        - Si el usuario saluda o hace un comentario genérico, responde de manera amistosa y ofrécele ayuda.
        - Si el mensaje es confuso, pide aclaraciones de forma educada.

        Usa un tono profesional, empático y accesible, como si fueras un asistente de clínica. Evita respuestas largas a menos que sea necesario, y siempre mantén el contexto de un sistema de reservas médicas. No inventes información específica sobre E-Dukate (como precios o ubicaciones) a menos que se proporcione. If the user mentions a technical issue or something beyond your scope, suggest contacting E-Dukate support.

        Mensaje del usuario: {message}";

        var payload = new
        {
            model = _modelName,
            prompt = prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending request to Ollama: {Payload}", json);

        var response = await _httpClient.PostAsync("/api/generate", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Raw Ollama JSON response: {Response}", responseJson);

        try
        {
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = ollamaResponse?.Response?.Trim();
            _logger.LogInformation("Deserialized Ollama response: {Result}", result ?? "null");
            return result ?? "Lo siento, no entendí tu mensaje. ¿Puedes proporcionar más detalles o decir 'reservar cita' para agendar una consulta?";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Ollama response: {Response}", responseJson);
            return "Lo siento, no entendí tu mensaje. ¿Puedes proporcionar más detalles o decir 'reservar cita' para agendar una consulta?";
        }
    }

    private class OllamaResponse
    {
        public string Response { get; set; } = string.Empty;
    }
}