namespace TourShuttle.Application.Common;

/// <summary>
/// Bound from the "Jwt" configuration section. Issuer, Audience and ExpiresMinutes live in appsettings.json;
/// Key is a secret and comes from user-secrets (development) or the Jwt__Key environment variable (Render).
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiresMinutes { get; set; }
    public string Key { get; set; } = default!;
}
