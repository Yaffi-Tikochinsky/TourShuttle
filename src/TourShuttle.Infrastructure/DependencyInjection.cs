using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourShuttle.Application.Interfaces;
using TourShuttle.Infrastructure.Persistence;
using TourShuttle.Infrastructure.Repositories;
using TourShuttle.Infrastructure.Security;

namespace TourShuttle.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers everything Infrastructure provides. Called once from Program.cs,
    /// which is the only place in the API that knows about this project.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is missing. In development set it with dotnet user-secrets; " +
                "in production set the ConnectionStrings__Default environment variable.");

        // AddDbContext registers AppDbContext as Scoped: one instance per HTTP request.
        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseAsyncSeeding((context, _, ct) => DbSeeder.SeedAsync(context, ct)));

        // Scoped, because they depend on the scoped AppDbContext.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDestinationRepository, DestinationRepository>();
        services.AddScoped<IPickupPointRepository, PickupPointRepository>();
        services.AddScoped<ITourRepository, TourRepository>();

        // Singletons are safe here: neither holds a scoped dependency (only IOptions or nothing).
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations, then runs the seeding hook, so the system starts
    /// on a fresh database without any manual step.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
