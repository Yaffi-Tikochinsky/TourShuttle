using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Mapping;
using TourShuttle.Application.Services;

namespace TourShuttle.Application;

public static class DependencyInjection
{
    /// <summary>Registers the application services and the AutoMapper profiles.</summary>
    /// <param name="autoMapperLicenseKey">
    /// Optional; when null AutoMapper reads the AUTOMAPPER_LICENSE_KEY environment variable.
    /// </param>
    public static IServiceCollection AddApplication(this IServiceCollection services, string? autoMapperLicenseKey)
    {
        services.AddAutoMapper(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(autoMapperLicenseKey))
            {
                cfg.LicenseKey = autoMapperLicenseKey;
            }
        }, typeof(UserProfile).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDestinationService, DestinationService>();
        services.AddScoped<IPickupPointService, PickupPointService>();
        services.AddScoped<ITourService, TourService>();

        // Singletons: no per-request state, and no scoped dependencies (only IOptions).
        services.AddSingleton<IPickupTimeEstimator, PickupTimeEstimator>();
        // The clock is injected, so tests can freeze "now" instead of depending on the real time.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
