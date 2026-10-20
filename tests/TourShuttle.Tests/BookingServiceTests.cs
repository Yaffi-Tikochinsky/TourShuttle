using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Bookings;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Tests;

/// <summary>
/// The limited resource. The repositories and the unit of work are mocks, so each test decides
/// exactly what the "database" returns and whether saving succeeds, conflicts or breaks a unique index.
/// </summary>
public class BookingServiceTests
{
    private const int CustomerId = 3;
    private const int OtherCustomerId = 4;
    private const int AdminId = 2;

    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<ITourRepository> _tours = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private Booking? _added;
    private Tour? _savedTour;

    public BookingServiceTests()
    {
        _bookings.Setup(b => b.Add(It.IsAny<Booking>())).Callback<Booking>(b => _added = b);

        // After a successful save the service reads the booking back with its tour and pickup point.
        _bookings.Setup(b => b.GetDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                _added!.Tour = _savedTour!;
                _added.TourPickupPoint = _savedTour!.PickupPoints.Single(p => p.PickupPointId == _added.PickupPointId);
                return _added;
            });
    }

    private BookingService CreateService() => new(_bookings.Object, _tours.Object, _unitOfWork.Object,
        TestData.Estimator(), TestData.Mapper(), TestData.Clock(), NullLogger<BookingService>.Instance);

    /// <summary>Every read of the tour returns this one (no other request interferes).</summary>
    private void TourInDatabase(Tour tour)
    {
        _savedTour = tour;
        _tours.Setup(t => t.GetForUpdateAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
    }

    private static BookingRequest Request(int pickupPointId, int seats) =>
        new() { PickupPointId = pickupPointId, SeatsCount = seats };

    private static ConcurrencyConflictException Conflict() => new(new Exception("stale Version"));

    private void VerifyNothingSaved() =>
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

    // ---------- Booking ----------

    [Fact]
    public async Task Valid_booking_takes_the_seats_and_fixes_the_price()
    {
        var tour = TestData.Tour(capacity: 3, price: 70m);
        TourInDatabase(tour);

        var result = await CreateService().CreateAsync(tour.Id, CustomerId, Request(pickupPointId: 2, seats: 2), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, tour.AvailableSeats);
        Assert.Equal(140m, result.Value!.TotalPrice);
        Assert.Equal(BookingStatus.Active, result.Value.Status);
        Assert.Equal(CustomerId, _added!.UserId);
        // Point 2 is the second stop; 12:00 in Israel means 2 minutes between stops.
        Assert.Equal(tour.DepartureAt.AddMinutes(2), result.Value.EstimatedPickupTimeUtc);
        // Seats and booking are saved together, in one SaveChanges.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Booking_shows_the_pickup_time_from_the_tours_own_minutes()
    {
        var tour = TestData.Tour(capacity: 10, route: [1, 2, 3], minutesBetweenPickupPoints: 4);
        TourInDatabase(tour);

        var result = await CreateService().CreateAsync(tour.Id, CustomerId, Request(pickupPointId: 3, seats: 1), default);

        // Third stop: departure + 2 × 4 minutes (the band would have given 2 × 2).
        Assert.Equal(tour.DepartureAt.AddMinutes(8), result.Value!.EstimatedPickupTimeUtc);
    }

    [Fact]
    public async Task More_seats_than_available_is_a_conflict_and_nothing_is_saved()
    {
        var tour = TestData.Tour(capacity: 3, availableSeats: 1);
        TourInDatabase(tour);

        var result = await CreateService().CreateAsync(tour.Id, CustomerId, Request(1, seats: 2), default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("Not enough seats", result.Error);
        Assert.Equal(1, tour.AvailableSeats);
        _bookings.Verify(b => b.Add(It.IsAny<Booking>()), Times.Never);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task More_seats_than_allowed_per_booking_is_a_validation_error()
    {
        TourInDatabase(TestData.Tour(capacity: 10, maxSeatsPerBooking: 3));

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, seats: 4), default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Pickup_point_that_is_not_on_the_route_is_a_validation_error()
    {
        TourInDatabase(TestData.Tour(route: [1, 2]));

        var result = await CreateService().CreateAsync(5, CustomerId, Request(pickupPointId: 9, seats: 1), default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Unknown_tour_is_not_found()
    {
        _tours.Setup(t => t.GetForUpdateAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Tour?)null);

        var result = await CreateService().CreateAsync(99, CustomerId, Request(1, 1), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Cancelled_tour_cannot_be_booked()
    {
        TourInDatabase(TestData.Tour(status: TourStatus.Cancelled));

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, 1), default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Tour_that_already_left_cannot_be_booked()
    {
        TourInDatabase(TestData.Tour(departureAt: TestData.Now.AddHours(-1)));

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, 1), default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Second_active_booking_for_the_same_tour_is_a_conflict()
    {
        TourInDatabase(TestData.Tour());
        _bookings.Setup(b => b.HasActiveBookingAsync(5, CustomerId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, 1), default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Unique_index_violation_from_a_simultaneous_double_booking_is_a_conflict()
    {
        TourInDatabase(TestData.Tour());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintException(new Exception("duplicate key")));

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, 1), default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
    }

    // ---------- Optimistic concurrency and retries ----------

    [Fact]
    public async Task Concurrency_conflict_is_retried_with_fresh_data_and_succeeds_when_seats_remain()
    {
        // Our first read saw 3 seats; meanwhile someone booked 1, so the save conflicts.
        var staleRead = TestData.Tour(capacity: 3, availableSeats: 3);
        var freshRead = TestData.Tour(capacity: 3, availableSeats: 2);
        _savedTour = freshRead;
        _tours.SetupSequence(t => t.GetForUpdateAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleRead)
            .ReturnsAsync(freshRead);
        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(Conflict())
            .Returns(Task.CompletedTask);

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, seats: 2), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, freshRead.AvailableSeats);
        _tours.Verify(t => t.GetForUpdateAsync(5, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
    }

    [Fact]
    public async Task Concurrency_conflict_ends_in_not_enough_seats_when_the_retry_finds_them_gone()
    {
        // The idea test: 3 seats left, we ask for 2, another family took all 3 first.
        var staleRead = TestData.Tour(capacity: 3, availableSeats: 3);
        var freshRead = TestData.Tour(capacity: 3, availableSeats: 0);
        _tours.SetupSequence(t => t.GetForUpdateAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleRead)
            .ReturnsAsync(freshRead);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(Conflict());

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, seats: 2), default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("Not enough seats", result.Error);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Conflict_on_every_attempt_gives_up_after_the_maximum_attempts()
    {
        _tours.Setup(t => t.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(() => TestData.Tour());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(Conflict());

        var result = await CreateService().CreateAsync(5, CustomerId, Request(1, 1), default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("taken by other customers", result.Error);
        _tours.Verify(t => t.GetForUpdateAsync(5, It.IsAny<CancellationToken>()), Times.Exactly(BookingService.MaxAttempts));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(BookingService.MaxAttempts));
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Exactly(BookingService.MaxAttempts));
    }

    // ---------- Cancelling ----------

    private Booking BookingInDatabase(int ownerId, BookingStatus status = BookingStatus.Active, DateTime? departureAt = null)
    {
        var tour = TestData.Tour(capacity: 10, availableSeats: 8, departureAt: departureAt);
        var booking = new Booking
        {
            Id = 27, TourId = tour.Id, Tour = tour, UserId = ownerId, PickupPointId = 1,
            SeatsCount = 2, TotalPrice = 140m, CreatedAt = TestData.Now.AddDays(-1), Status = status
        };
        _bookings.Setup(b => b.GetForUpdateAsync(27, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        return booking;
    }

    [Fact]
    public async Task Cancelling_another_customers_booking_is_forbidden()
    {
        var booking = BookingInDatabase(ownerId: CustomerId);

        var result = await CreateService().CancelAsync(27, OtherCustomerId, UserRole.Customer, default);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal(BookingStatus.Active, booking.Status);
        Assert.Equal(8, booking.Tour.AvailableSeats);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Owner_cancels_and_the_seats_go_back_to_the_tour()
    {
        var booking = BookingInDatabase(ownerId: CustomerId);

        var result = await CreateService().CancelAsync(27, CustomerId, UserRole.Customer, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(10, booking.Tour.AvailableSeats);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Admin_can_cancel_any_booking()
    {
        var booking = BookingInDatabase(ownerId: CustomerId);

        var result = await CreateService().CancelAsync(27, AdminId, UserRole.Admin, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public async Task Cancelling_twice_is_a_conflict()
    {
        BookingInDatabase(ownerId: CustomerId, status: BookingStatus.Cancelled);

        var result = await CreateService().CancelAsync(27, CustomerId, UserRole.Customer, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Booking_cannot_be_cancelled_after_the_tour_left()
    {
        BookingInDatabase(ownerId: CustomerId, departureAt: TestData.Now.AddHours(-1));

        var result = await CreateService().CancelAsync(27, CustomerId, UserRole.Customer, default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        VerifyNothingSaved();
    }
}
