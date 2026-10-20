using Microsoft.EntityFrameworkCore;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;
using TourShuttle.Infrastructure.Security;

namespace TourShuttle.Infrastructure.Persistence;

/// <summary>
/// Demo data, so the system can be tried right after it starts. Runs on every startup;
/// each part checks its own condition, so it only adds what is missing and never deletes anything.
/// </summary>
public static class DbSeeder
{
    private static readonly TimeZoneInfo IsraelTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jerusalem");

    public static async Task SeedAsync(DbContext context, CancellationToken ct)
    {
        await SeedCatalogAsync(context, ct);
        await SeedUsersAsync(context, ct);
        await SeedDemoToursAsync(context, ct);
    }

    /// <summary>Tour sites and pickup points, on an empty database.</summary>
    private static async Task SeedCatalogAsync(DbContext context, CancellationToken ct)
    {
        if (await context.Set<Destination>().AnyAsync(ct))
        {
            return;
        }

        // Inserted in this order, so the ids are known: destinations 1-3, pickup points 1-5.
        context.AddRange(
            new Destination { Name = "הכותל המערבי", City = "ירושלים", Description = "תפילה וסיור ברחבת הכותל ובמנהרות הכותל" },
            new Destination { Name = "עיר דוד", City = "ירושלים", Description = "סיור בעיר דוד העתיקה ובנקבת השילוח" },
            new Destination { Name = "מגדל דוד", City = "ירושלים", Description = "מוזיאון ותצפית על העיר העתיקה" });
        await context.SaveChangesAsync(ct);

        context.AddRange(
            new PickupPoint { Name = "בני ברק - רבי עקיבא", Address = "רבי עקיבא 100, בני ברק", City = "בני ברק" },
            new PickupPoint { Name = "רמת גן - הבורסה", Address = "ז'בוטינסקי 7, רמת גן", City = "רמת גן" },
            new PickupPoint { Name = "פתח תקווה - צומת סגולה", Address = "ז'בוטינסקי 130, פתח תקווה", City = "פתח תקווה" },
            new PickupPoint { Name = "מודיעין - מרכז העיר", Address = "עמק דותן 1, מודיעין", City = "מודיעין" },
            new PickupPoint { Name = "בית שמש - רמה א'", Address = "נהר הירדן 1, בית שמש", City = "בית שמש" });
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Demo accounts for every role. The passwords are public on purpose: the course requires
    /// demo users with their credentials in the README.
    /// </summary>
    private static async Task SeedUsersAsync(DbContext context, CancellationToken ct)
    {
        if (await context.Set<User>().AnyAsync(ct))
        {
            return;
        }

        // The seeder runs outside the request pipeline, so it creates the stateless hasher directly.
        var hasher = new PasswordHasher();

        User NewUser(string fullName, string email, string phone, string password, UserRole role) => new()
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = hasher.Hash(password),
            Role = role
        };

        context.AddRange(
            NewUser("מנהל המערכת", "admin@tourshuttle.demo", "050-0000000", "Admin123!", UserRole.Admin),
            NewUser("ישראל ישראלי", "customer1@tourshuttle.demo", "052-1111111", "Customer123!", UserRole.Customer),
            NewUser("שרה כהן", "customer2@tourshuttle.demo", "052-2222222", "Customer123!", UserRole.Customer),
            NewUser("משה הנהג", "driver1@tourshuttle.demo", "050-1111111", "Driver123!", UserRole.Driver),
            NewUser("אהרון הנהג", "driver2@tourshuttle.demo", "050-2222222", "Driver123!", UserRole.Driver));
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Demo tours relative to today. Tours age: a set seeded once would all be in the past a week later,
    /// and booking (or the 409 demo) could no longer be shown. So whenever no upcoming tour is left,
    /// a fresh set is added. Old tours and their bookings stay.
    /// </summary>
    private static async Task SeedDemoToursAsync(DbContext context, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (await context.Set<Tour>().AnyAsync(t => t.Status == TourStatus.Scheduled && t.DepartureAt > now, ct))
        {
            return;
        }

        var destinations = await context.Set<Destination>().OrderBy(d => d.Id).Take(3).ToListAsync(ct);
        var points = await context.Set<PickupPoint>().OrderBy(p => p.Id).Take(5).ToListAsync(ct);
        var drivers = await context.Set<User>().Where(u => u.Role == UserRole.Driver).OrderBy(u => u.Id).ToListAsync(ct);
        if (destinations.Count < 3 || points.Count < 5)
        {
            // An admin removed parts of the demo catalog; there is nothing sensible to build tours from.
            return;
        }

        var (westernWall, cityOfDavid, towerOfDavid) = (destinations[0], destinations[1], destinations[2]);
        var (bneiBrak, ramatGan, petahTikva, modiin, beitShemesh) = (points[0], points[1], points[2], points[3], points[4]);

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
        // The 09:00 tour would get 2 minutes from its band; a custom value shows the override winning.
        tours[0].MinutesBetweenPickupPoints = 4;

        // Alternate the drivers, so each driver has tours of their own
        // and a 403 can be shown when a driver opens another driver's passenger list.
        for (var i = 0; i < tours.Count && drivers.Count > 0; i++)
        {
            tours[i].Driver = drivers[i % drivers.Count];
        }

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
