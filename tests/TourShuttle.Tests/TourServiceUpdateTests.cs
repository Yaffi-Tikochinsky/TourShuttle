using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Tests;

/// <summary>
/// Editing a tour (decision 4 of stage 4): price, driver, seats per booking and capacity can change at any time
/// (capacity never below the booked seats); destination, departure and route are locked once bookings exist.
/// </summary>
public class TourServiceUpdateTests
{
    private const int DriverId = 5;
    private const int CustomerId = 3;

    private readonly Mock<ITourRepository> _tours = new();
    private readonly Mock<IDestinationRepository> _destinations = new();
    private readonly Mock<IPickupPointRepository> _pickupPoints = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    public TourServiceUpdateTests()
    {
        _destinations.Setup(d => d.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Destination { Id = id, Name = "Site", City = "City" });
        _users.Setup(u => u.GetByIdAsync(DriverId, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.User(DriverId, UserRole.Driver));
        _users.Setup(u => u.GetByIdAsync(CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.User(CustomerId, UserRole.Customer));
        _pickupPoints.Setup(p => p.CountExistingAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) => ids.Count);
        // The mocked transaction just runs the work, like the real one does when nothing fails.
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<Task> work, CancellationToken _) => work());
    }

    private TourService CreateService() => new(_tours.Object, _destinations.Object, _pickupPoints.Object,
        _users.Object, _unitOfWork.Object, TestData.Estimator(), TestData.Mapper(), TestData.Clock(),
        NullLogger<TourService>.Instance);

    /// <summary>Capacity 10 with 4 seats left: 6 seats are booked.</summary>
    private Tour TourInDatabase(bool hasBookings)
    {
        var tour = TestData.Tour(capacity: 10, availableSeats: 4, maxSeatsPerBooking: 3, driverId: DriverId, route: [1, 2]);
        _tours.Setup(t => t.GetForUpdateAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        _tours.Setup(t => t.HasBookingsAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(hasBookings);
        return tour;
    }

    /// <summary>A PUT body that changes nothing; each test changes the one field it is about.</summary>
    private static TourRequest Unchanged(Tour tour) => new()
    {
        DestinationId = tour.DestinationId,
        DriverId = tour.DriverId,
        DepartureAt = tour.DepartureAt,
        ReturnAt = tour.ReturnAt,
        Capacity = tour.Capacity,
        MaxSeatsPerBooking = tour.MaxSeatsPerBooking,
        Price = tour.Price,
        PickupPointIds = tour.PickupPoints.OrderBy(p => p.Order).Select(p => p.PickupPointId).ToList()
    };

    private void VerifyNothingSaved() =>
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task Capacity_cannot_drop_below_the_seats_already_booked()
    {
        var tour = TourInDatabase(hasBookings: true);
        var request = Unchanged(tour);
        request.Capacity = 5;

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("6 seats are already booked", result.Error);
        Assert.Equal(10, tour.Capacity);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Raising_capacity_keeps_the_booked_seats_taken()
    {
        var tour = TourInDatabase(hasBookings: true);
        var request = Unchanged(tour);
        request.Capacity = 20;

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, tour.Capacity);
        Assert.Equal(14, tour.AvailableSeats);
    }

    [Fact]
    public async Task Price_can_change_after_bookings_exist()
    {
        var tour = TourInDatabase(hasBookings: true);
        var request = Unchanged(tour);
        request.Price = 99m;

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(99m, tour.Price);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Route_is_locked_once_bookings_exist()
    {
        var tour = TourInDatabase(hasBookings: true);
        var request = Unchanged(tour);
        request.PickupPointIds = [2, 1];

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Departure_is_locked_once_bookings_exist()
    {
        var tour = TourInDatabase(hasBookings: true);
        var request = Unchanged(tour);
        request.DepartureAt = tour.DepartureAt.AddHours(1);
        request.ReturnAt = tour.ReturnAt.AddHours(1);

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Route_without_bookings_is_replaced_inside_a_transaction()
    {
        var tour = TourInDatabase(hasBookings: false);
        var request = Unchanged(tour);
        request.PickupPointIds = [2, 1];

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.True(result.IsSuccess);
        Assert.Equal([2, 1], tour.PickupPoints.OrderBy(p => p.Order).Select(p => p.PickupPointId));
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        // Old rows deleted in the first save, new rows inserted in the second.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Date_without_a_time_zone_is_refused()
    {
        var tour = TourInDatabase(hasBookings: false);
        var request = Unchanged(tour);
        request.DepartureAt = DateTime.SpecifyKind(tour.DepartureAt, DateTimeKind.Unspecified);

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Only_a_driver_can_be_assigned_as_the_driver()
    {
        var tour = TourInDatabase(hasBookings: false);
        var request = Unchanged(tour);
        request.DriverId = CustomerId;

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        Assert.Contains("not a driver", result.Error);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Concurrent_change_by_someone_else_is_a_conflict()
    {
        var tour = TourInDatabase(hasBookings: true);
        var request = Unchanged(tour);
        request.Price = 99m;
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(new Exception("stale Version")));

        var result = await CreateService().UpdateAsync(tour.Id, request, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }
}
