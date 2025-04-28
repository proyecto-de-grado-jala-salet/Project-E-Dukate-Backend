using E_Dukate.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace E_Dukate.Presentation.Configuration;

public static class DatabaseConfiguration
{
    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                x => x.MigrationsAssembly("E-Dukate.Infrastructure")));

        return services;
    }
}