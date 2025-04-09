using DotNetEnv;
using E_Dukate.Presentation.Configuration;

Env.Load("../.env");

var builder = WebApplication.CreateBuilder(args);

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