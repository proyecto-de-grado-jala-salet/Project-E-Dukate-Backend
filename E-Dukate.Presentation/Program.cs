using E_Dukate.Presentation.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services
    .ConfigureCors()
    .ConfigureDatabase(builder.Configuration)
    .AddApplicationServices()
    .ConfigureSwagger()
    .AddControllers();

var app = builder.Build();

app.UseCorsConfiguration()
   .ConfigureMiddleware(app.Environment);

app.Run();