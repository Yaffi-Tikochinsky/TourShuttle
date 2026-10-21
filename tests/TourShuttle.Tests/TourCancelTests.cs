using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TourShuttle.Application.Common;
using TourShuttle.Application.Interfaces;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Tests;

/// <summary>An admin cancels a tour: the tour and all its active bookings are cancelled together.</summary>
public class TourCancelTests
{
    private readonly Mock<ITourRepository> _tours = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private TourService CreateService() => new(_tours.Object, Mock.Of<IDestinationRepository>(), Mock.Of<IPickupPointRepository>(),
        Mock.Of<IUserRepository>(), _unitOfWork.Object, TestData.Estimator(), TestData.Mapper(), TestData.Clock(),
        NullLogger<TourService>.Instance);

    /// <summary>Capacity 10; two active bookings of 2 and 3 seats, so 5 seats are left.</summary>
    private Tour TourWithBookings(DateTime? departureAt = null, TourStatus status = TourStatus.Scheduled)
    {
        var tour = TestData.Tour(capacity: 10, availableSeats: 5, departureAt: departureAt, status: status);
        tour.Bookings.Add(new Booking { Id = 1, TourId = tour.Id, SeatsCount = 2, Status = BookingStatus.Active });
        tour.Bookings.Add(new Booking { Id = 2, TourId = tour.Id, SeatsCount = 3, Status = BookingStatus.Active });
        _tours.Setup(t => t.GetWithActiveBookingsForUpdateAsync(tour.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tour);
        return tour;
    }

    [Fact]
    public async Task Cancelling_cancels_the_tour_and_every_active_booking_in_one_save()
    {
        var tour = TourWithBookings();

        var result = await CreateService().CancelAsync(tour.Id, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
        Assert.Equal(TourStatus.Cancelled, tour.Status);
        Assert.All(tour.Bookings, b => Assert.Equal(BookingStatus.Cancelled, b.Status));
        Assert.Equal(tour.Capacity, tour.AvailableSeats);
        // One SaveChanges is one transaction: the tour never ends up cancelled with live bookings.
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_tour_that_already_left_cannot_be_cancelled()
    {
        var tour = TourWithBookings(departureAt: TestData.Now.AddMinutes(-10));

        var result = await CreateService().CancelAsync(tour.Id, default);

        Assert.Equal(ResultStatus.Validation, result.Status);
        Assert.Equal(TourStatus.Scheduled, tour.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancelling_twice_is_a_conflict()
    {
        var tour = TourWithBookings(status: TourStatus.Cancelled);

        var result = await CreateService().CancelAsync(tour.Id, default);

        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task A_booking_saved_at_the_same_moment_makes_the_cancel_read_again_and_retry()
    {
        // Each read returns a fresh copy, as the database does after DiscardChanges. The second copy has a third
        // booking: the one that was saved at the same moment and caused the conflict.
        var stale = TourWithBookings();
        var fresh = TourWithBookings();
        fresh.Bookings.Add(new Booking { Id = 3, TourId = fresh.Id, SeatsCount = 1, Status = BookingStatus.Active });
        _tours.SetupSequence(t => t.GetWithActiveBookingsForUpdateAsync(stale.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stale)
            .ReturnsAsync(fresh);
        // The first save meets that booking's change to the tour's Version; the second goes through.
        _unitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException(new Exception("version changed")))
            .Returns(Task.CompletedTask);

        var result = await CreateService().CancelAsync(stale.Id, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
        Assert.All(fresh.Bookings, b => Assert.Equal(BookingStatus.Cancelled, b.Status));
        _unitOfWork.Verify(u => u.DiscardChanges(), Times.Once);
    }

    [Fact]
    public async Task An_unknown_tour_is_not_found()
    {
        var result = await CreateService().CancelAsync(999, default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
