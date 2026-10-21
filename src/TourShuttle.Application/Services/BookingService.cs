using AutoMapper;
using Microsoft.Extensions.Logging;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Bookings;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Services;

public class BookingService : IBookingService
{
    /// <summary>
    /// How many times to re-read the tour after another request changed it first.
    /// Each retry checks the seats again, so a conflict ends in 409 only when the seats are really gone.
    /// </summary>
    public const int MaxAttempts = 3;

    private const string AlreadyBooked = "You already have an active booking for this tour. Cancel it to book again.";

    private readonly IBookingRepository _bookings;
    private readonly ITourRepository _tours;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPickupTimeEstimator _estimator;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IBookingRepository bookings, ITourRepository tours, IUnitOfWork unitOfWork,
        IPickupTimeEstimator estimator, IMapper mapper, TimeProvider time, ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _tours = tours;
        _unitOfWork = unitOfWork;
        _estimator = estimator;
        _mapper = mapper;
        _time = time;
        _logger = logger;
    }

    public async Task<Result<BookingResponse>> CreateAsync(int tourId, int customerId, BookingRequest request,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // Tracked, so the seat change below is saved together with the new booking.
            var tour = await _tours.GetForUpdateAsync(tourId, ct);
            if (tour is null)
            {
                return Result<BookingResponse>.NotFound($"Tour {tourId} was not found.");
            }

            if (tour.Status == TourStatus.Cancelled || tour.DepartureAt <= UtcNow())
            {
                return Result<BookingResponse>.Validation("The tour is not open for booking: it was cancelled or has already left.");
            }

            if (request.SeatsCount > tour.MaxSeatsPerBooking)
            {
                return Result<BookingResponse>.Validation($"A booking can have at most {tour.MaxSeatsPerBooking} seats on this tour.");
            }

            if (tour.PickupPoints.All(p => p.PickupPointId != request.PickupPointId))
            {
                return Result<BookingResponse>.Validation($"Pickup point {request.PickupPointId} is not on this tour's route.");
            }

            if (await _bookings.HasActiveBookingAsync(tourId, customerId, ct))
            {
                return Result<BookingResponse>.Conflict(AlreadyBooked);
            }

            if (tour.AvailableSeats < request.SeatsCount)
            {
                // A conflict over seats like the version conflict below, only noticed one step earlier
                // (the other booking had already committed when this one read the tour): logged the same way.
                _logger.LogWarning("Not enough seats on tour {TourId}: {AvailableSeats} left, {RequestedSeats} requested",
                    tourId, tour.AvailableSeats, request.SeatsCount);
                return Result<BookingResponse>.Conflict(
                    $"Not enough seats: {tour.AvailableSeats} left and {request.SeatsCount} requested.");
            }

            // Check and change in the same unit of work: the UPDATE of the tour carries the Version we read,
            // so if anyone saved this tour after our read, the UPDATE matches no row and nothing is saved.
            tour.AvailableSeats -= request.SeatsCount;
            var booking = new Booking
            {
                TourId = tourId,
                UserId = customerId,
                PickupPointId = request.PickupPointId,
                SeatsCount = request.SeatsCount,
                TotalPrice = tour.Price * request.SeatsCount,
                CreatedAt = UtcNow(),
                Status = BookingStatus.Active
            };
            _bookings.Add(booking);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning("Seat conflict on tour {TourId}: another request saved it first (attempt {Attempt} of {MaxAttempts})",
                    tourId, attempt, MaxAttempts);
                // Drop our stale copy and the unsaved booking, then read the tour again.
                _unitOfWork.DiscardChanges();
                continue;
            }
            catch (UniqueConstraintException)
            {
                // The same customer sent two bookings at once; the partial unique index let one in.
                _unitOfWork.DiscardChanges();
                return Result<BookingResponse>.Conflict(AlreadyBooked);
            }

            _logger.LogInformation("Booking {BookingId} created: tour {TourId}, {SeatsCount} seats, customer {UserId}",
                booking.Id, tourId, request.SeatsCount, customerId);
            return Result<BookingResponse>.Success(await LoadResponseAsync(booking.Id, ct));
        }

        _logger.LogWarning("Booking on tour {TourId} gave up after {MaxAttempts} conflicting attempts", tourId, MaxAttempts);
        return Result<BookingResponse>.Conflict("The seats were taken by other customers at the same moment. Please try again.");
    }

    public async Task<Result<BookingResponse>> GetByIdAsync(int id, int requesterId, UserRole requesterRole,
        CancellationToken ct)
    {
        var booking = await _bookings.GetDetailsAsync(id, ct);
        if (booking is null)
        {
            return Result<BookingResponse>.NotFound(NotFoundMessage(id));
        }

        if (!CanAccess(booking, requesterId, requesterRole))
        {
            return Result<BookingResponse>.Forbidden("You can only view your own bookings.");
        }

        return Result<BookingResponse>.Success(ToResponse(booking));
    }

    public async Task<IReadOnlyList<BookingResponse>> ListForCustomerAsync(int customerId, CancellationToken ct)
    {
        var bookings = await _bookings.ListForUserAsync(customerId, ct);
        return bookings.Select(ToResponse).ToList();
    }

    public async Task<Result> CancelAsync(int id, int requesterId, UserRole requesterRole, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var booking = await _bookings.GetForUpdateAsync(id, ct);
            if (booking is null)
            {
                return Result.NotFound(NotFoundMessage(id));
            }

            if (!CanAccess(booking, requesterId, requesterRole))
            {
                return Result.Forbidden("You can only cancel your own bookings.");
            }

            if (booking.Status == BookingStatus.Cancelled)
            {
                return Result.Conflict("The booking is already cancelled.");
            }

            if (booking.Tour.DepartureAt <= UtcNow())
            {
                return Result.Validation("The tour has already left, so the booking can no longer be cancelled.");
            }

            // Soft delete: the row stays as history, and the seats go back to the tour in the same save.
            booking.Status = BookingStatus.Cancelled;
            booking.Tour.AvailableSeats += booking.SeatsCount;

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (ConcurrencyConflictException)
            {
                _logger.LogWarning("Seat conflict on tour {TourId} while cancelling booking {BookingId} (attempt {Attempt} of {MaxAttempts})",
                    booking.TourId, id, attempt, MaxAttempts);
                _unitOfWork.DiscardChanges();
                continue;
            }

            _logger.LogInformation("Booking {BookingId} cancelled by user {UserId}", id, requesterId);
            return Result.Success();
        }

        return Result.Conflict("The tour was changed by other requests at the same moment. Please try again.");
    }

    // Resource-based authorization: being signed in is not enough, the booking must be yours (or you are an admin).
    private static bool CanAccess(Booking booking, int requesterId, UserRole requesterRole) =>
        booking.UserId == requesterId || requesterRole == UserRole.Admin;

    private async Task<BookingResponse> LoadResponseAsync(int id, CancellationToken ct) =>
        ToResponse((await _bookings.GetDetailsAsync(id, ct))!);

    private BookingResponse ToResponse(Booking booking)
    {
        var response = _mapper.Map<BookingResponse>(booking);
        response.TourDisplayStatus = TourDisplayStatusCalculator.Calculate(
            booking.Tour.Status, booking.Tour.DepartureAt, booking.Tour.ReturnAt, UtcNow());
        // One booking has one pickup point, so the interval is asked for once here too.
        var minutes = _estimator.GetMinutesBetweenPoints(booking.Tour.DepartureAt, booking.Tour.MinutesBetweenPickupPoints);
        response.EstimatedPickupTimeUtc = _estimator.EstimatePickupTime(
            booking.Tour.DepartureAt, booking.TourPickupPoint.Order, minutes);
        return response;
    }

    private DateTime UtcNow() => _time.GetUtcNow().UtcDateTime;

    private static string NotFoundMessage(int id) => $"Booking {id} was not found.";
}
