using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TourShuttle.Application.Common;
using TourShuttle.Application.Interfaces;

namespace TourShuttle.Application.Services;

/// <summary>
/// Registered as a singleton: it depends only on IOptions and ILogger (both singletons)
/// and keeps no per-request state.
/// </summary>
public class PickupTimeEstimator : IPickupTimeEstimator
{
    private readonly PickupTimeEstimationSettings _settings;
    private readonly TimeZoneInfo _timeZone;
    private readonly ILogger<PickupTimeEstimator> _logger;

    public PickupTimeEstimator(IOptions<PickupTimeEstimationSettings> options, ILogger<PickupTimeEstimator> logger)
    {
        _settings = options.Value;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZone);
        _logger = logger;
    }

    public int GetMinutesBetweenPoints(DateTime departureAtUtc, int? tourMinutes)
    {
        if (tourMinutes is > 0)
        {
            _logger.LogDebug("Pickup interval {Minutes} min: set on the tour", tourMinutes.Value);
            return tourMinutes.Value;
        }

        // The bands describe Israeli traffic, so the check uses Israel's clock, not UTC.
        var localTime = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(departureAtUtc, _timeZone));

        var band = _settings.Bands.FirstOrDefault(b => b.Contains(localTime));
        if (band is null)
        {
            _logger.LogDebug("Pickup interval {Minutes} min: departure {LocalTime} (Israel time) is in no band, using the default",
                _settings.DefaultMinutes, localTime);
            return _settings.DefaultMinutes;
        }

        _logger.LogDebug("Pickup interval {Minutes} min: departure {LocalTime} (Israel time) is in the {From}-{To} band",
            band.Minutes, localTime, band.From, band.To);
        return band.Minutes;
    }

    public DateTime EstimatePickupTime(DateTime departureAtUtc, int order, int minutesBetweenPoints) =>
        departureAtUtc.AddMinutes((order - 1) * minutesBetweenPoints);
}
