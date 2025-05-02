namespace E_Dukate.Application.Interfaces.WhatsApp;

public interface IDialogflowService
{
    Task<string> DetectIntentAsync(string sessionId, string message);
}
