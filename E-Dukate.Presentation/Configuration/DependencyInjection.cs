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
using E_Dukate.Application.Services.WhatsApp;
using E_Dukate.Infrastructure.Services;
using E_Dukate.Application.DTOs.Schedules;
using E_Dukate.Application.Interfaces.WhatsApp;
using E_Dukate.Application.Interfaces.GoogleCalendar;
using E_Dukate.Application.Services.WhatsApp.Handlers;
using E_Dukate.Application.Services.WhatsApp.Utilities;
using E_Dukate.Infrastructure.Services.GoogleCalendar;
using E_Dukate.Application.Services.Auth;
using E_Dukate.Application.Validators.Auth;
using E_Dukate.Application.DTOs.Auth;
using E_Dukate.Domain.Entities.Auth;
using E_Dukate.Application.Interfaces.Auth;
using E_Dukate.Domain.Entities.MedicalHistories;
using E_Dukate.Application.Services.MedicalHistories;
using E_Dukate.Application.DTOs.MedicalHistories;
using E_Dukate.Application.Validators.MedicalHistories;
using E_Dukate.Application.Validators.Schedule;

namespace E_Dukate.Presentation.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services
            .AddRepositories()
            .AddServices()
            .AddValidators()
            .AddChatBotServices()
            .AddGoogleCalendarServices();
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IGenericRepository<Administrator>, GenericRepository<Administrator>>();
        services.AddScoped<IGenericRepository<Specialist>, GenericRepository<Specialist>>();
        services.AddScoped<IGenericRepository<Patient>, GenericRepository<Patient>>();
        services.AddScoped<IGenericRepository<Specialty>, GenericRepository<Specialty>>();
        services.AddScoped<IGenericRepository<Schedule>, GenericRepository<Schedule>>();
        services.AddScoped<IGenericRepository<TimeSlot>, GenericRepository<TimeSlot>>();
        services.AddScoped<IGenericRepository<LoginLog>, GenericRepository<LoginLog>>();
        services.AddScoped<IGenericRepository<UserAuth>, GenericRepository<UserAuth>>();
        services.AddScoped<IGenericRepository<MedicalHistory>, GenericRepository<MedicalHistory>>();
        services.AddScoped<IGenericRepository<MedicalHistoryPermission>, GenericRepository<MedicalHistoryPermission>>();
        services.AddScoped<IGenericRepository<MedicalConsultation>, GenericRepository<MedicalConsultation>>();
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
        services.AddScoped<AuthService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<LoginLogger>();
        services.AddScoped<MedicalHistoryService>();
        services.AddScoped<MedicalConsultationService>();
        services.AddScoped<StartAppointmentHandler>();
        services.AddScoped<AskNameHandler>();
        services.AddScoped<AskLastNamePaternalHandler>();
        services.AddScoped<AskIdentityCardHandler>();
        services.AddScoped<AskDateOfBirthHandler>();
        services.AddScoped<AskGenderHandler>();
        services.AddScoped<AskAddressHandler>();
        services.AddScoped<ShowSpecialtiesHandler>();
        services.AddScoped<ShowSpecialistsHandler>();
        services.AddScoped<ShowSchedulesHandler>();
        services.AddSingleton<IConversationStateManager, ConversationStateManager>();
        return services;
    }

    private static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<AdministratorDto>, AdministratorValidator>();
        services.AddScoped<IValidator<SpecialistDto>, SpecialistValidator>();
        services.AddScoped<IValidator<PatientDto>, PatientValidator>();
        services.AddScoped<IValidator<SpecialtyDto>, SpecialtyValidator>();
        services.AddScoped<IValidator<ScheduleDto>, ScheduleValidator>();
        services.AddScoped<IValidator<LoginDto>, LoginValidator>();
        services.AddScoped<IValidator<UpdateMedicalConsultationDto>, UpdateMedicalConsultationDtoValidator>();
        services.AddScoped<UserAuthValidator>();
        return services;
    }

    private static IServiceCollection AddChatBotServices(this IServiceCollection services)
    {
        services.AddSingleton<IChatBotService, ChatBotService>();
        services.AddSingleton<IWhatsAppService, WhatsAppService>();
        services.AddSingleton<IDialogflowService, DialogflowService>();
        services.AddHttpClient();
        return services;
    }

    private static IServiceCollection AddGoogleCalendarServices(this IServiceCollection services)
    {
        services.AddSingleton<IGoogleCalendarService, GoogleCalendarService>();
        return services;
    }
}