using E_Dukate.Application.Services.Users;
using E_Dukate.Application.Services.Specialties;
using E_Dukate.Domain.Interfaces;
using E_Dukate.Infrastructure.Repositories;
using E_Dukate.Domain.Entities.Users;
using E_Dukate.Domain.Entities.Specialties;
using E_Dukate.Application.Validators.Users;
using E_Dukate.Application.Validators.Specialties;
using E_Dukate.Application.DTOs.Users;
using E_Dukate.Application.DTOs.Specialties;
using FluentValidation;
using E_Dukate.Domain.Entities.Schedules;
using E_Dukate.Application.Services;
using E_Dukate.Application.DTOs.Schedules;
using E_Dukate.Application.Validators;

namespace E_Dukate.Presentation.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddRepositories()
            .AddServices()
            .AddValidators();
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<Administrator>, GenericRepository<Administrator>>();
        services.AddScoped<IGenericRepository<Specialist>, GenericRepository<Specialist>>();
        services.AddScoped<IGenericRepository<Patient>, GenericRepository<Patient>>();
        services.AddScoped<IGenericRepository<Specialty>, GenericRepository<Specialty>>();
        services.AddScoped<IGenericRepository<Schedule>, GenericRepository<Schedule>>(); // Agregar Schedule
        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<AdministratorService>();
        services.AddScoped<SpecialistService>();
        services.AddScoped<PatientService>();
        services.AddScoped<SpecialtyService>();
        services.AddScoped<UserService>();
        services.AddScoped<ScheduleService>();
        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<AdministratorDto>, AdministratorValidator>();
        services.AddScoped<IValidator<SpecialistDto>, SpecialistValidator>();
        services.AddScoped<IValidator<PatientDto>, PatientValidator>();
        services.AddScoped<IValidator<SpecialtyDto>, SpecialtyValidator>();
        services.AddScoped<IValidator<ScheduleDto>, ScheduleValidator>();
        return services;
    }
}