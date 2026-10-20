using Microsoft.Extensions.Options;
using TourShuttle.Application.Common;
using TourShuttle.Application.Interfaces;

namespace TourShuttle.Application.Services;

/// <summary>
/// Registered as a singleton: it depends only on IOptions (also a singleton) and keeps no per-request state.
/// </summary>
public class PickupTimeEstimator : IPickupTimeEstimator
{
    private readonly PickupTimeEstimationSettings _settings;
    private readonly TimeZoneInfo _timeZone;

    public PickupTimeEstimator(IOptions<PickupTimeEstimationSettings> options)
    {
        _settings = options.Value;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZone);
    }

    public int GetMinutesBetweenPoints(DateTime departureAtUtc, int? tourMinutes)
    {
        if (tourMinutes is > 0)
        {
            return tourMinutes.Value;
        }

        // The bands describe Israeli traffic, so the check uses Israel's clock, not UTC.
        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(departureAtUtc, _timeZone));

        var band = _settings.Bands.FirstOrDefault(b => b.Contains(localTime));
        return band?.Minutes ?? _settings.DefaultMinutes;
    }

    public DateTime EstimatePickupTime(DateTime departureAtUtc, int order, int? tourMinutes) =>
        departureAtUtc.AddMinutes((order - 1) * GetMinutesBetweenPoints(departureAtUtc, tourMinutes));
}
