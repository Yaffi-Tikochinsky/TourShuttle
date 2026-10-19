using Microsoft.EntityFrameworkCore;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Infrastructure.Persistence;

/// <summary>
/// Demo data for an empty database, so the system can be tried right after it starts.
/// Tour times are relative to "now" so the demo tours are always in the future.
/// Users are seeded in stage 3, once password hashing exists.
/// </summary>
public static class DbSeeder
{
    private static readonly TimeZoneInfo IsraelTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    public static async Task SeedAsync(DbContext context, CancellationToken ct)
    {
        if (await context.Set<Destination>().AnyAsync(ct))
        {
            return;
        }

        var westernWall = new Destination { Name = "הכותל המערבי", City = "ירושלים", Description = "תפילה וסיור ברחבת הכותל ובמנהרות הכותל" };
        var cityOfDavid = new Destination { Name = "עיר דוד", City = "ירושלים", Description = "סיור בעיר דוד העתיקה ובנקבת השילוח" };
        var towerOfDavid = new Destination { Name = "מגדל דוד", City = "ירושלים", Description = "מוזיאון ותצפית על העיר העתיקה" };

        var bneiBrak = new PickupPoint { Name = "בני ברק - רבי עקיבא", Address = "רבי עקיבא 100, בני ברק" };
        var ramatGan = new PickupPoint { Name = "רמת גן - הבורסה", Address = "ז'בוטינסקי 7, רמת גן" };
        var petahTikva = new PickupPoint { Name = "פתח תקווה - צומת סגולה", Address = "ז'בוטינסקי 130, פתח תקווה" };
        var modiin = new PickupPoint { Name = "מודיעין - מרכז העיר", Address = "עמק דותן 1, מודיעין" };
        var beitShemesh = new PickupPoint { Name = "בית שמש - רמה א'", Address = "נהר הירדן 1, בית שמש" };

        // Departure times cover the three pickup-time bands: night (23:00-07:30),
        // rush hour (14:30-17:30) and regular hours.
        var tours = new List<Tour>
        {
            NewTour(westernWall, IsraelLocal(1, 9, 0), hours: 5, capacity: 50, maxPerBooking: 8, price: 45m,
                bneiBrak, ramatGan, petahTikva),
            NewTour(westernWall, IsraelLocal(1, 23, 30), hours: 3, capacity: 40, maxPerBooking: 6, price: 50m,
                bneiBrak, petahTikva, modiin),
            NewTour(cityOfDavid, IsraelLocal(2, 15, 0), hours: 5, capacity: 40, maxPerBooking: 6, price: 60m,
                ramatGan, bneiBrak, modiin),
            NewTour(cityOfDavid, IsraelLocal(3, 6, 0), hours: 6, capacity: 30, maxPerBooking: 5, price: 60m,
                beitShemesh, modiin),
            // Only 3 seats: two customers booking at the same moment demonstrate the 409 conflict.
            NewTour(towerOfDavid, IsraelLocal(4, 10, 0), hours: 4, capacity: 3, maxPerBooking: 3, price: 70m,
                bneiBrak, ramatGan),
            NewTour(towerOfDavid, IsraelLocal(5, 16, 0), hours: 4, capacity: 30, maxPerBooking: 6, price: 70m,
                petahTikva, bneiBrak, beitShemesh, modiin, ramatGan),
        };
        tours[^1].Status = TourStatus.Cancelled;

        context.AddRange(tours);
        await context.SaveChangesAsync(ct);
    }

    private static Tour NewTour(Destination destination, DateTime departureAt, int hours, int capacity,
        int maxPerBooking, decimal price, params PickupPoint[] route)
    {
        var tour = new Tour
        {
            Destination = destination,
            DepartureAt = departureAt,
            ReturnAt = departureAt.AddHours(hours),
            Capacity = capacity,
            AvailableSeats = capacity,
            MaxSeatsPerBooking = maxPerBooking,
            Price = price,
        };

        for (var i = 0; i < route.Length; i++)
        {
            tour.PickupPoints.Add(new TourPickupPoint { PickupPoint = route[i], Order = i + 1 });
        }

        return tour;
    }

    /// <summary>Israel local time <paramref name="daysFromToday"/> days from today, converted to UTC.</summary>
    private static DateTime IsraelLocal(int daysFromToday, int hour, int minute)
    {
        var todayInIsrael = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IsraelTime).Date;
        var local = todayInIsrael.AddDays(daysFromToday).AddHours(hour).AddMinutes(minute);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), IsraelTime);
    }
}
