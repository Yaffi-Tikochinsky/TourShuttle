using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;
using TourShuttle.Infrastructure.Persistence;
using TourShuttle.Infrastructure.Repositories;

namespace TourShuttle.Tests;

/// <summary>
/// Searching tours by the city a customer leaves from. The repository tests run the real SQL
/// (EXISTS over the route, ProjectTo of the route's cities) on SQLite in memory.
/// </summary>
public sealed class TourCityFilterTests : IDisposable
{
    private const string BneiBrak = "בני ברק";
    private const string RamatGan = "רמת גן";
    private const string Modiin = "מודיעין";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly Dictionary<string, int> _tourIds = [];

    public TourCityFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        using var db = new AppDbContext(_options);
        db.Database.EnsureCreated();

        var rabbiAkiva = new PickupPoint { Name = "רבי עקיבא", Address = "רבי עקיבא 100", City = BneiBrak };
        var jabotinsky = new PickupPoint { Name = "ז'בוטינסקי", Address = "ז'בוטינסקי 10", City = BneiBrak };
        var bourse = new PickupPoint { Name = "הבורסה", Address = "ז'בוטינסקי 7", City = RamatGan };
        var center = new PickupPoint { Name = "מרכז העיר", Address = "עמק דותן 1", City = Modiin };
        // One destination shared by every tour: destination names are unique.
        var wall = new Destination { Name = "הכותל המערבי", City = "ירושלים" };

        // Two stops in Bnei Brak, then Ramat Gan: the city list must keep route order and list Bnei Brak once.
        var coastal = AddTour(db, wall, daysAhead: 1, TourStatus.Scheduled, rabbiAkiva, jabotinsky, bourse);
        var modiin = AddTour(db, wall, daysAhead: 2, TourStatus.Scheduled, center);
        var cancelled = AddTour(db, wall, daysAhead: 3, TourStatus.Cancelled, bourse);
        db.SaveChanges();

        _tourIds["coastal"] = coastal.Id;
        _tourIds["modiin"] = modiin.Id;
        _tourIds["cancelled"] = cancelled.Id;
    }

    public void Dispose() => _connection.Dispose();

    private static Tour AddTour(AppDbContext db, Destination destination, int daysAhead, TourStatus status, params PickupPoint[] route)
    {
        var departure = TestData.Now.AddDays(daysAhead);
        var tour = new Tour
        {
            Destination = destination,
            DepartureAt = departure,
            ReturnAt = departure.AddHours(4),
            Capacity = 10,
            AvailableSeats = 10,
            MaxSeatsPerBooking = 3,
            Price = 50m,
            Status = status
        };
        for (var i = 0; i < route.Length; i++)
        {
            tour.PickupPoints.Add(new TourPickupPoint { PickupPoint = route[i], Order = i + 1 });
        }

        db.Tours.Add(tour);
        return tour;
    }

    private async Task<IReadOnlyList<TourSummaryResponse>> ListAsync(string? city, bool includeCancelled = false)
    {
        await using var db = new AppDbContext(_options);
        var repository = new TourRepository(db, TestData.Mapper());
        var filter = new TourListFilter(null, city, TestData.Now, null, TourSortBy.Departure, false, includeCancelled, 0, 50);
        var (items, _) = await repository.ListAsync(filter, default);
        return items;
    }

    private int Id(string key) => _tourIds[key];

    [Fact]
    public async Task City_filter_returns_every_tour_with_a_stop_there()
    {
        var fromRamatGan = await ListAsync(RamatGan, includeCancelled: true);

        // Ramat Gan is the coastal tour's third stop, not its first: it still counts.
        Assert.Equal([Id("coastal"), Id("cancelled")], fromRamatGan.Select(t => t.Id));
    }

    [Fact]
    public async Task City_filter_combines_with_the_other_filters()
    {
        var fromRamatGan = await ListAsync(RamatGan, includeCancelled: false);

        Assert.Equal([Id("coastal")], fromRamatGan.Select(t => t.Id));
    }

    [Fact]
    public async Task City_with_no_stops_returns_nothing_and_no_city_returns_everything()
    {
        Assert.Empty(await ListAsync("חיפה"));
        Assert.Equal([Id("coastal"), Id("modiin")], (await ListAsync(null)).Select(t => t.Id));
    }

    [Fact]
    public async Task Route_cities_come_back_in_route_order()
    {
        var coastal = (await ListAsync(BneiBrak)).Single();

        // One entry per stop from the database; TourService removes the repeat.
        Assert.Equal([BneiBrak, BneiBrak, RamatGan], coastal.Cities);
    }

    [Fact]
    public async Task Service_lists_each_city_once_keeping_the_route_order()
    {
        await using var db = new AppDbContext(_options);
        var service = new TourService(new TourRepository(db, TestData.Mapper()), Mock.Of<IDestinationRepository>(),
            Mock.Of<IPickupPointRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IUnitOfWork>(),
            TestData.Estimator(), TestData.Mapper(), TestData.Clock(), NullLogger<TourService>.Instance);

        var page = (await service.ListAsync(new TourQueryParameters { City = $"  {BneiBrak} " }, isAdmin: false, default)).Value!;

        Assert.Equal([BneiBrak, RamatGan], page.Items.Single().Cities);
    }

    [Fact]
    public async Task City_list_has_each_city_once_in_alphabetical_order()
    {
        await using var db = new AppDbContext(_options);

        var cities = await new PickupPointRepository(db).ListCitiesAsync(default);

        Assert.Equal([BneiBrak, Modiin, RamatGan], cities);
    }
}
