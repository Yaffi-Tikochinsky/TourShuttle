using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Tests;

/// <summary>"In progress" and "completed" are computed from the clock, never stored.</summary>
public class TourDisplayStatusCalculatorTests
{
    private static readonly DateTime Departure = new(2026, 10, 25, 7, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Return = Departure.AddHours(4);

    [Theory]
    [InlineData(-60, TourDisplayStatus.Scheduled)]   // an hour before departure
    [InlineData(0, TourDisplayStatus.InProgress)]    // exactly at departure
    [InlineData(120, TourDisplayStatus.InProgress)]  // on the way
    [InlineData(240, TourDisplayStatus.Completed)]   // exactly at return
    [InlineData(600, TourDisplayStatus.Completed)]
    public void Scheduled_tour_status_follows_the_clock(int minutesAfterDeparture, TourDisplayStatus expected)
    {
        var now = Departure.AddMinutes(minutesAfterDeparture);

        Assert.Equal(expected, TourDisplayStatusCalculator.Calculate(TourStatus.Scheduled, Departure, Return, now));
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(120)]
    [InlineData(600)]
    public void Cancelled_tour_is_cancelled_at_any_time(int minutesAfterDeparture)
    {
        var now = Departure.AddMinutes(minutesAfterDeparture);

        Assert.Equal(TourDisplayStatus.Cancelled,
            TourDisplayStatusCalculator.Calculate(TourStatus.Cancelled, Departure, Return, now));
    }
}
