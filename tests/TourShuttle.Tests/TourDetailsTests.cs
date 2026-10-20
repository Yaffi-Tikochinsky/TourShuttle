using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Tests;

/// <summary>GET /api/tours/{id}: estimated times, the stored minutes value, and what non-admins see.</summary>
public class TourDetailsTests
{
    private readonly Mock<ITourRepository> _tours = new();

    private TourService CreateService() => new(_tours.Object, Mock.Of<IDestinationRepository>(),
        Mock.Of<IPickupPointRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IUnitOfWork>(),
        TestData.Estimator(), TestData.Mapper(), TestData.Clock(), NullLogger<TourService>.Instance);

    private Tour TourInDatabase(int? minutesBetweenPickupPoints)
    {
        // Departs at 12:00 Israel time: the band alone would give 2 minutes.
        var tour = TestData.Tour(driverId: 5, route: [1, 2, 3], minutesBetweenPickupPoints: minutesBetweenPickupPoints);
        _tours.Setup(t => t.GetDetailsAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        return tour;
    }

    [Fact]
    public async Task Minutes_set_on_the_tour_drive_the_estimated_times()
    {
        var tour = TourInDatabase(minutesBetweenPickupPoints: 5);

        var details = (await CreateService().GetByIdAsync(tour.Id, isAdmin: false, default)).Value!;

        Assert.Equal(5, details.MinutesBetweenPickupPoints);
        Assert.Equal(5, details.CustomMinutesBetweenPickupPoints);
        Assert.Equal(
            [tour.DepartureAt, tour.DepartureAt.AddMinutes(5), tour.DepartureAt.AddMinutes(10)],
            details.PickupPoints.Select(p => p.EstimatedPickupTimeUtc));
    }

    [Fact]
    public async Task Without_a_stored_value_the_band_decides_and_the_stored_field_stays_empty()
    {
        var tour = TourInDatabase(minutesBetweenPickupPoints: null);

        var details = (await CreateService().GetByIdAsync(tour.Id, isAdmin: false, default)).Value!;

        // The effective value (an edit form's placeholder) and the stored one (its value) differ here.
        Assert.Equal(2, details.MinutesBetweenPickupPoints);
        Assert.Null(details.CustomMinutesBetweenPickupPoints);
        Assert.Equal(tour.DepartureAt.AddMinutes(4), details.PickupPoints[2].EstimatedPickupTimeUtc);
    }

    [Fact]
    public async Task Driver_id_is_shown_to_admins_only()
    {
        var tour = TourInDatabase(minutesBetweenPickupPoints: null);
        var service = CreateService();

        var asCustomer = (await service.GetByIdAsync(tour.Id, isAdmin: false, default)).Value!;
        var asAdmin = (await service.GetByIdAsync(tour.Id, isAdmin: true, default)).Value!;

        Assert.Null(asCustomer.DriverId);
        Assert.Equal(5, asAdmin.DriverId);
        Assert.Equal(asAdmin.DriverName, asCustomer.DriverName);
    }
}
