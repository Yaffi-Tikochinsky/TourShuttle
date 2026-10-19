using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}
