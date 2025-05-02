using Microsoft.AspNetCore.Mvc;
using E_Dukate.Application.DTOs.WhatsApp;
using E_Dukate.Application.Interfaces.WhatsApp;

namespace E_Dukate.Presentation.Controllers.WhatsApp;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly IChatBotService _chatBotService;
    private const string VerifyToken = "edukate";

    public WebhookController(IChatBotService chatBotService)
    {
        _chatBotService = chatBotService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string verifyToken)
    {
        if (mode == "subscribe" && verifyToken == VerifyToken)
        {
            return Ok(challenge);
        }
        return Unauthorized();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessWebhook([FromBody] WebhookResponseDto webhookResponse)
    {
        if (webhookResponse?.Entry == null || !webhookResponse.Entry.Any())
        {
            return BadRequest("Invalid webhook payload.");
        }

        var entry = webhookResponse.Entry.FirstOrDefault();
        var change = entry?.Changes?.FirstOrDefault();
        var message = change?.Value?.Messages?.FirstOrDefault();

        if (message == null)
        {
            return Ok();
        }

        string incomingMessage = message.Type switch
        {
            "text" when !string.IsNullOrEmpty(message.Text?.Body) => message.Text.Body,
            "interactive" when !string.IsNullOrEmpty(message.Interactive?.ListReply?.Title) => message.Interactive.ListReply.Title,
            "interactive" when !string.IsNullOrEmpty(message.Interactive?.ButtonReply?.Title) => message.Interactive.ButtonReply.Title,
            _ => ""
        };
        
        string fromPhone = message.From;

        if (!string.IsNullOrEmpty(incomingMessage))
        {
            await _chatBotService.ProcessMessageAsync(fromPhone, incomingMessage);
        }

        return Ok();
    }
}