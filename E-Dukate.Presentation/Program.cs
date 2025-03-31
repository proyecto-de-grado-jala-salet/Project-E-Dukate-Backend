using E_Dukate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using E_Dukate.Application.Services;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Infrastructure.Repositories;
using E_Dukate.Domain.Entities;
using E_Dukate.Application.Validators;
using E_Dukate.Application.DTOs;
using FluentValidation;
using DotNetEnv;

Env.Load(); // Cargar variables de entorno desde .env

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IGenericRepository<Administrator>, GenericRepository<Administrator>>();
builder.Services.AddScoped<IGenericRepository<Specialist>, GenericRepository<Specialist>>();
builder.Services.AddScoped<IGenericRepository<Patient>, GenericRepository<Patient>>();
builder.Services.AddScoped<AdministratorService>();
builder.Services.AddScoped<SpecialistService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<IValidator<AdministratorDto>, AdministratorValidator>();
builder.Services.AddScoped<IValidator<SpecialistDto>, SpecialistValidator>();
builder.Services.AddScoped<IValidator<PatientDto>, PatientValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();