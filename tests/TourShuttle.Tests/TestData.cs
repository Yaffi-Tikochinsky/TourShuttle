using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TourShuttle.Application.Common;
using TourShuttle.Application.Mapping;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Tests;

/// <summary>Builders shared by the tests, so each test shows only the values that matter to it.</summary>
public static class TestData
{
    /// <summary>"Now" in every service test: 21.10.2026 09:00 UTC, which is 12:00 in Israel.</summary>
    public static readonly DateTime Now = new(2026, 10, 21, 9, 0, 0, DateTimeKind.Utc);

    private static readonly TimeZoneInfo IsraelTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    /// <summary>The application's real AutoMapper profiles. A mocked mapper would only return what we told it to.</summary>
    public static readonly MapperConfiguration MapperConfiguration =
        new(cfg => cfg.AddMaps(typeof(UserProfile).Assembly), NullLoggerFactory.Instance);

    public static IMapper Mapper() => MapperConfiguration.CreateMapper();

    /// <summary>A clock frozen at <see cref="Now"/>.</summary>
    public static FakeTimeProvider Clock() => new(new DateTimeOffset(Now));

    /// <summary>The same bands as appsettings.json.</summary>
    public static PickupTimeEstimationSettings EstimationSettings() => new()
    {
        TimeZone = "Asia/Jerusalem",
        DefaultMinutes = 2,
        Bands =
        [
            new TimeBand { From = new TimeOnly(23, 0), To = new TimeOnly(7, 30), Minutes = 1 },
            new TimeBand { From = new TimeOnly(14, 30), To = new TimeOnly(17, 30), Minutes = 3 }
        ]
    };

    public static PickupTimeEstimator Estimator() =>
        new(Options.Create(EstimationSettings()), NullLogger<PickupTimeEstimator>.Instance);

    /// <summary>An Israel wall-clock time as UTC, with the right offset for that date (winter or summer time).</summary>
    public static DateTime IsraelToUtc(int year, int month, int day, int hour, int minute) =>
        TimeZoneInfo.ConvertTimeToUtc(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified), IsraelTime);

    /// <summary>
    /// A tour three days from <see cref="Now"/> (09:00 UTC = 12:00 Israel, so 2 minutes between stops),
    /// with its destination and route loaded, as the repositories return it.
    /// </summary>
    public static Tour Tour(int id = 5, int capacity = 3, int? availableSeats = null, int maxSeatsPerBooking = 3,
        decimal price = 70m, DateTime? departureAt = null, TourStatus status = TourStatus.Scheduled,
        int? driverId = null, int[]? route = null, int? minutesBetweenPickupPoints = null)
    {
        var departure = departureAt ?? Now.AddDays(3);
        var tour = new Tour
        {
            Id = id,
            DestinationId = 3,
            Destination = new Destination { Id = 3, Name = "מגדל דוד", City = "ירושלים", Description = "מוזיאון" },
            DriverId = driverId,
            DepartureAt = departure,
            ReturnAt = departure.AddHours(4),
            Capacity = capacity,
            AvailableSeats = availableSeats ?? capacity,
            MaxSeatsPerBooking = maxSeatsPerBooking,
            Price = price,
            MinutesBetweenPickupPoints = minutesBetweenPickupPoints,
            Status = status
        };

        var points = route ?? [1, 2];
        for (var i = 0; i < points.Length; i++)
        {
            tour.PickupPoints.Add(new TourPickupPoint
            {
                TourId = id,
                Tour = tour,
                PickupPointId = points[i],
                PickupPoint = new PickupPoint
                {
                    Id = points[i], Name = $"Point {points[i]}", Address = $"Street {points[i]}", City = $"City {points[i]}"
                },
                Order = i + 1
            });
        }

        return tour;
    }

    public static User User(int id, UserRole role, string? fullName = null) => new()
    {
        Id = id,
        FullName = fullName ?? $"User {id}",
        Email = $"user{id}@test.demo",
        Phone = $"050-000000{id}",
        PasswordHash = "stored-hash",
        Role = role
    };
}
