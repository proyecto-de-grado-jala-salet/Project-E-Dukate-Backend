using Google.Cloud.Dialogflow.V2;
using E_Dukate.Application.Interfaces.WhatsApp;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;

namespace E_Dukate.Infrastructure.Services;

public class DialogflowService : IDialogflowService
{
    private readonly SessionsClient _sessionsClient;
    private readonly string _projectId;

    public DialogflowService(IConfiguration configuration)
    {
        _projectId = configuration["Dialogflow:ProjectId"] ?? throw new ArgumentNullException("Dialogflow:ProjectId is missing.");
        var credentialsPath = configuration["Dialogflow:CredentialsPath"] ?? throw new ArgumentNullException("Dialogflow:CredentialsPath is missing.");

        var credential = GoogleCredential.FromFile(credentialsPath);

        var builder = new SessionsClientBuilder
        {
            Credential = credential
        };
        _sessionsClient = builder.Build();
    }

    public async Task<string> DetectIntentAsync(string sessionId, string message)
    {
        var session = SessionName.FromProjectSession(_projectId, sessionId);
        var queryInput = new QueryInput
        {
            Text = new TextInput
            {
                Text = message,
                LanguageCode = "es"
            }
        };

        var response = await _sessionsClient.DetectIntentAsync(session, queryInput);
        return response.QueryResult.Intent?.DisplayName ?? string.Empty;
    }
}