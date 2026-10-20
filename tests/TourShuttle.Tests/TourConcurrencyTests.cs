using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TourShuttle.Application.Common;
using TourShuttle.Domain.Entities;
using TourShuttle.Infrastructure.Persistence;

namespace TourShuttle.Tests;

/// <summary>
/// The plan's "tested separately!" case: proof that Tour.Version stops a lost update, with the order of
/// events fixed by the test instead of left to timing. Two AppDbContext instances play two customers.
/// SQLite in memory runs real SQL, so EF sends a real UPDATE ... WHERE "Version" = @original and counts the rows
/// it changed, exactly as against PostgreSQL. This is why the plan chose a Guid Version over PostgreSQL's xmin.
/// </summary>
public sealed class TourConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly int _tourId;

    public TourConcurrencyTests()
    {
        // An in-memory SQLite database lives as long as its connection stays open, so every context shares this one.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();
        var departure = TestData.Now.AddDays(3);
        var tour = new Tour
        {
            Destination = new Destination { Name = "מגדל דוד", City = "ירושלים" },
            DepartureAt = departure,
            ReturnAt = departure.AddHours(4),
            Capacity = 3,
            AvailableSeats = 3,
            MaxSeatsPerBooking = 3,
            Price = 70m
        };
        db.Tours.Add(tour);
        db.SaveChanges();
        _tourId = tour.Id;
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext NewContext() => new(_options);

    [Fact]
    public async Task Second_save_based_on_a_stale_read_is_rejected()
    {
        await using var familyA = NewContext();
        await using var familyB = NewContext();

        // Both read the tour before either saves: 3 seats, the same Version.
        var tourA = await familyA.Tours.SingleAsync(t => t.Id == _tourId);
        var tourB = await familyB.Tours.SingleAsync(t => t.Id == _tourId);
        Assert.Equal(tourA.Version, tourB.Version);

        tourA.AvailableSeats -= 3;
        await familyA.SaveChangesAsync();

        tourB.AvailableSeats -= 2;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => familyB.SaveChangesAsync());

        // Family B's update was not applied: 0 seats, never -2.
        await using var check = NewContext();
        Assert.Equal(0, (await check.Tours.SingleAsync(t => t.Id == _tourId)).AvailableSeats);
    }

    [Fact]
    public async Task UnitOfWork_turns_the_EF_exception_into_the_application_exception()
    {
        await using var familyA = NewContext();
        await using var familyB = NewContext();
        var tourA = await familyA.Tours.SingleAsync(t => t.Id == _tourId);
        var tourB = await familyB.Tours.SingleAsync(t => t.Id == _tourId);

        tourA.AvailableSeats -= 3;
        await new UnitOfWork(familyA).SaveChangesAsync(default);
        tourB.AvailableSeats -= 2;

        // BookingService catches this one; it never sees EF Core.
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new UnitOfWork(familyB).SaveChangesAsync(default));
    }

    [Fact]
    public async Task Every_save_of_a_changed_tour_renews_its_Version()
    {
        await using var db = NewContext();
        var tour = await db.Tours.SingleAsync(t => t.Id == _tourId);
        var before = tour.Version;

        tour.AvailableSeats -= 1;
        await db.SaveChangesAsync();

        Assert.NotEqual(before, tour.Version);
    }

    [Fact]
    public async Task After_discarding_changes_a_retry_reads_the_fresh_row()
    {
        await using var familyA = NewContext();
        await using var familyB = NewContext();
        var tourA = await familyA.Tours.SingleAsync(t => t.Id == _tourId);
        var tourB = await familyB.Tours.SingleAsync(t => t.Id == _tourId);
        tourA.AvailableSeats -= 3;
        await familyA.SaveChangesAsync();
        tourB.AvailableSeats -= 2;
        var unitOfWork = new UnitOfWork(familyB);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => unitOfWork.SaveChangesAsync(default));

        // What BookingService does before its next attempt.
        unitOfWork.DiscardChanges();
        var reread = await familyB.Tours.SingleAsync(t => t.Id == _tourId);

        // The retry sees the real state (0 seats) and will answer "not enough seats" instead of overbooking.
        Assert.Equal(0, reread.AvailableSeats);
        Assert.Equal(tourA.Version, reread.Version);
    }
}
