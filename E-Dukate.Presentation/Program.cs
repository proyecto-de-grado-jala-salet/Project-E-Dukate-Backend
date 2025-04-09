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

Env.Load("../.env"); // Cargar .env desde la raíz de la solución

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Agregar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database configuration
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IGenericRepository<Administrator>, GenericRepository<Administrator>>();
builder.Services.AddScoped<IGenericRepository<Specialist>, GenericRepository<Specialist>>();
builder.Services.AddScoped<IGenericRepository<Patient>, GenericRepository<Patient>>();
builder.Services.AddScoped<IGenericRepository<Specialty>, GenericRepository<Specialty>>();
builder.Services.AddScoped<AdministratorService>();
builder.Services.AddScoped<SpecialistService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<SpecialtyService>();
builder.Services.AddScoped<IValidator<AdministratorDto>, AdministratorValidator>();
builder.Services.AddScoped<IValidator<SpecialistDto>, SpecialistValidator>();
builder.Services.AddScoped<IValidator<PatientDto>, PatientValidator>();
builder.Services.AddScoped<IValidator<SpecialtyDto>, SpecialtyValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();