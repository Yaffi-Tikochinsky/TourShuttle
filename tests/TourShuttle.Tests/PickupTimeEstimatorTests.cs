namespace TourShuttle.Tests;

/// <summary>
/// The pickup-time calculation. No mocks: the estimator is pure computation over its settings.
/// Bands (Israel time): 23:00-07:30 → 1 min, 14:30-17:30 → 3 min, otherwise 2 min.
/// </summary>
public class PickupTimeEstimatorTests
{
    [Theory]
    // January: Israel is UTC+2 (winter time).
    [InlineData(1, 23, 30, 1)] // night band, before midnight
    [InlineData(1, 6, 0, 1)]   // night band, after midnight: the case a naive From <= t < To check gets wrong
    [InlineData(1, 23, 0, 1)]  // From is inclusive
    [InlineData(1, 7, 29, 1)]
    [InlineData(1, 7, 30, 2)]  // To is exclusive
    [InlineData(1, 15, 0, 3)]  // rush hour
    [InlineData(1, 14, 30, 3)]
    [InlineData(1, 17, 29, 3)]
    [InlineData(1, 17, 30, 2)]
    [InlineData(1, 9, 0, 2)]   // no band: the default
    // July: Israel is UTC+3 (summer time). The same Israel times must give the same answers.
    [InlineData(7, 23, 30, 1)]
    [InlineData(7, 6, 0, 1)]
    [InlineData(7, 15, 0, 3)]
    [InlineData(7, 9, 0, 2)]
    public void Minutes_follow_the_band_of_the_Israel_departure_time(int month, int hour, int minute, int expected)
    {
        var departureUtc = TestData.IsraelToUtc(2026, month, 15, hour, minute);

        var minutes = TestData.Estimator().GetMinutesBetweenPoints(departureUtc, tourMinutes: null);

        Assert.Equal(expected, minutes);
    }

    [Fact]
    public void Same_UTC_hour_falls_in_different_bands_in_winter_and_summer()
    {
        // 20:30 UTC is 22:30 in January (no band → 2) but 23:30 in July (night band → 1).
        var estimator = TestData.Estimator();

        Assert.Equal(2, estimator.GetMinutesBetweenPoints(new DateTime(2026, 1, 15, 20, 30, 0, DateTimeKind.Utc), null));
        Assert.Equal(1, estimator.GetMinutesBetweenPoints(new DateTime(2026, 7, 15, 20, 30, 0, DateTimeKind.Utc), null));
    }

    [Fact]
    public void Minutes_set_on_the_tour_win_over_the_band()
    {
        var nightDeparture = TestData.IsraelToUtc(2026, 1, 15, 23, 30);

        Assert.Equal(5, TestData.Estimator().GetMinutesBetweenPoints(nightDeparture, tourMinutes: 5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Missing_or_zero_tour_minutes_fall_back_to_the_band(int? tourMinutes)
    {
        var rushHourDeparture = TestData.IsraelToUtc(2026, 1, 15, 15, 0);

        Assert.Equal(3, TestData.Estimator().GetMinutesBetweenPoints(rushHourDeparture, tourMinutes));
    }

    [Theory]
    [InlineData(1, 0)]  // the first stop is the departure time itself
    [InlineData(2, 3)]
    [InlineData(4, 9)]  // departure + (4 − 1) × 3
    public void Pickup_time_is_departure_plus_stops_before_it_times_minutes(int order, int expectedMinutesAfterDeparture)
    {
        var departure = new DateTime(2026, 10, 25, 5, 0, 0, DateTimeKind.Utc);

        var pickup = TestData.Estimator().EstimatePickupTime(departure, order, minutesBetweenPoints: 3);

        Assert.Equal(departure.AddMinutes(expectedMinutesAfterDeparture), pickup);
    }
}
