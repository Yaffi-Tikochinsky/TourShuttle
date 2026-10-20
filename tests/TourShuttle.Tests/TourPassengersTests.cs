using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.Common;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Tests;

/// <summary>The driver's pickup sheet: resource-based authorization and grouping by stop.</summary>
public class TourPassengersTests
{
    private const int AssignedDriverId = 5;
    private const int OtherDriverId = 6;

    private readonly Mock<ITourRepository> _tours = new();

    private TourService CreateService() => new(_tours.Object, Mock.Of<IDestinationRepository>(),
        Mock.Of<IPickupPointRepository>(), Mock.Of<IUserRepository>(), Mock.Of<IUnitOfWork>(),
        TestData.Estimator(), TestData.Mapper(), TestData.Clock(), NullLogger<TourService>.Instance);

    /// <summary>
    /// Route: point 4, then 1, then 2. Two bookings at point 1, one at point 4, none at point 2.
    /// Only active bookings, as the repository's filtered Include returns them.
    /// </summary>
    private Tour TourWithPassengers()
    {
        var tour = TestData.Tour(capacity: 10, availableSeats: 5, driverId: AssignedDriverId, route: [4, 1, 2]);

        void Book(int bookingId, int userId, string name, int pickupPointId, int seats) =>
            tour.Bookings.Add(new Booking
            {
                Id = bookingId, TourId = tour.Id, UserId = userId, User = TestData.User(userId, UserRole.Customer, name),
                PickupPointId = pickupPointId, SeatsCount = seats, Status = BookingStatus.Active
            });

        Book(1, 3, "Sara", pickupPointId: 1, seats: 2);
        Book(2, 7, "Avi", pickupPointId: 1, seats: 1);
        Book(3, 8, "Dana", pickupPointId: 4, seats: 2);

        _tours.Setup(t => t.GetWithPassengersAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        return tour;
    }

    [Fact]
    public async Task Driver_not_assigned_to_the_tour_is_forbidden()
    {
        TourWithPassengers();

        var result = await CreateService().GetPassengersAsync(5, OtherDriverId, UserRole.Driver, default);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Assigned_driver_gets_passengers_grouped_by_stop_in_route_order()
    {
        var tour = TourWithPassengers();

        var result = await CreateService().GetPassengersAsync(5, AssignedDriverId, UserRole.Driver, default);

        Assert.True(result.IsSuccess);
        var sheet = result.Value!;
        Assert.Equal([4, 1, 2], sheet.PickupPoints.Select(p => p.PickupPointId));
        Assert.Equal([2, 3, 0], sheet.PickupPoints.Select(p => p.TotalSeats));
        Assert.Equal(5, sheet.TotalSeats);
        Assert.Equal(["Avi", "Sara"], sheet.PickupPoints[1].Passengers.Select(p => p.FullName));
        Assert.Empty(sheet.PickupPoints[2].Passengers);
        // Second stop, 12:00 Israel time: 2 minutes after departure.
        Assert.Equal(tour.DepartureAt.AddMinutes(2), sheet.PickupPoints[1].EstimatedPickupTimeUtc);
    }

    [Fact]
    public async Task Admin_sees_any_tours_passengers()
    {
        TourWithPassengers();

        var result = await CreateService().GetPassengersAsync(5, requesterId: 2, UserRole.Admin, default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Customer_is_forbidden_even_if_the_role_check_were_missing()
    {
        TourWithPassengers();

        var result = await CreateService().GetPassengersAsync(5, requesterId: 3, UserRole.Customer, default);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Unknown_tour_is_not_found()
    {
        _tours.Setup(t => t.GetWithPassengersAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Tour?)null);

        var result = await CreateService().GetPassengersAsync(99, AssignedDriverId, UserRole.Driver, default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
