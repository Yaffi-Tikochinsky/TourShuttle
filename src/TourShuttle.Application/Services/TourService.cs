using AutoMapper;
using Microsoft.Extensions.Logging;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Services;

public class TourService : ITourService
{
    private const string LockedByBookings =
        "The tour has bookings, so its destination, departure time and route cannot change. Cancel it and create a new tour instead.";
    private const string ChangedByOthers = "The tour was changed by another request. Reload it and try again.";

    private readonly ITourRepository _tours;
    private readonly IDestinationRepository _destinations;
    private readonly IPickupPointRepository _pickupPoints;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPickupTimeEstimator _estimator;
    private readonly IMapper _mapper;
    private readonly TimeProvider _time;
    private readonly ILogger<TourService> _logger;

    public TourService(ITourRepository tours, IDestinationRepository destinations, IPickupPointRepository pickupPoints,
        IUserRepository users, IUnitOfWork unitOfWork, IPickupTimeEstimator estimator, IMapper mapper,
        TimeProvider time, ILogger<TourService> logger)
    {
        _tours = tours;
        _destinations = destinations;
        _pickupPoints = pickupPoints;
        _users = users;
        _unitOfWork = unitOfWork;
        _estimator = estimator;
        _mapper = mapper;
        _time = time;
        _logger = logger;
    }

    public async Task<Result<PagedResult<TourSummaryResponse>>> ListAsync(TourQueryParameters query, bool isAdmin,
        CancellationToken ct)
    {
        var now = UtcNow();
        var from = UtcDates.ToUtcLenient(query.From) ?? now;
        var to = UtcDates.ToUtcLenient(query.To);
        if (to <= from)
        {
            return Result<PagedResult<TourSummaryResponse>>.Validation("'to' must be later than 'from'.");
        }

        var filter = new TourListFilter(
            query.DestinationId,
            from,
            to,
            query.SortBy,
            Descending: query.SortDirection == SortDirection.Desc,
            IncludeCancelled: query.IncludeCancelled && isAdmin,
            Skip: (query.Page - 1) * query.PageSize,
            Take: query.PageSize);

        var (items, totalCount) = await _tours.ListAsync(filter, ct);
        foreach (var item in items)
        {
            item.DisplayStatus = TourDisplayStatusCalculator.Calculate(item.Status, item.DepartureAt, item.ReturnAt, now);
        }

        return Result<PagedResult<TourSummaryResponse>>.Success(
            new PagedResult<TourSummaryResponse>(items, query.Page, query.PageSize, totalCount));
    }

    public async Task<Result<TourDetailsResponse>> GetByIdAsync(int id, bool isAdmin, CancellationToken ct)
    {
        var tour = await _tours.GetDetailsAsync(id, ct);
        if (tour is null)
        {
            return Result<TourDetailsResponse>.NotFound(NotFoundMessage(id));
        }

        var response = _mapper.Map<TourDetailsResponse>(tour);
        response.DisplayStatus = TourDisplayStatusCalculator.Calculate(tour.Status, tour.DepartureAt, tour.ReturnAt, UtcNow());

        // The tour has no minutes override yet (stage 8 adds the column), so it passes null.
        response.MinutesBetweenPickupPoints = _estimator.GetMinutesBetweenPoints(tour.DepartureAt, null);
        foreach (var point in response.PickupPoints)
        {
            point.EstimatedPickupTimeUtc = _estimator.EstimatePickupTime(tour.DepartureAt, point.Order, null);
        }

        if (!isAdmin)
        {
            response.DriverId = null;
        }

        return Result<TourDetailsResponse>.Success(response);
    }

    public async Task<Result<TourDetailsResponse>> CreateAsync(TourRequest request, CancellationToken ct)
    {
        var (error, departureAt, returnAt) = await ValidateAsync(request, ct);
        if (error is not null)
        {
            return Result<TourDetailsResponse>.Validation(error);
        }

        if (departureAt <= UtcNow())
        {
            return Result<TourDetailsResponse>.Validation("The departure time must be in the future.");
        }

        var tour = new Tour
        {
            DestinationId = request.DestinationId,
            DriverId = request.DriverId,
            DepartureAt = departureAt,
            ReturnAt = returnAt,
            Capacity = request.Capacity,
            AvailableSeats = request.Capacity,
            MaxSeatsPerBooking = request.MaxSeatsPerBooking,
            Price = request.Price
        };
        AddRoute(tour, request.PickupPointIds);

        _tours.Add(tour);
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Tour {TourId} created", tour.Id);

        // Read it back with names and pickup times, exactly as GET returns it.
        return await GetByIdAsync(tour.Id, isAdmin: true, ct);
    }

    public async Task<Result> UpdateAsync(int id, TourRequest request, CancellationToken ct)
    {
        var tour = await _tours.GetForUpdateAsync(id, ct);
        if (tour is null)
        {
            return Result.NotFound(NotFoundMessage(id));
        }

        var (error, departureAt, returnAt) = await ValidateAsync(request, ct);
        if (error is not null)
        {
            return Result.Validation(error);
        }

        var departureChanged = tour.DepartureAt != departureAt;
        if (departureChanged && departureAt <= UtcNow())
        {
            return Result.Validation("The departure time must be in the future.");
        }

        var currentRoute = tour.PickupPoints.OrderBy(p => p.Order).Select(p => p.PickupPointId);
        var routeChanged = !currentRoute.SequenceEqual(request.PickupPointIds);
        var destinationChanged = tour.DestinationId != request.DestinationId;

        // Customers booked this destination, this time and this pickup point: those fields are locked once bookings exist.
        if ((destinationChanged || departureChanged || routeChanged) && await _tours.HasBookingsAsync(id, ct))
        {
            return Result.Conflict(LockedByBookings);
        }

        var bookedSeats = tour.Capacity - tour.AvailableSeats;
        if (request.Capacity < bookedSeats)
        {
            return Result.Conflict($"{bookedSeats} seats are already booked, so the capacity cannot be lower than that.");
        }

        tour.DestinationId = request.DestinationId;
        tour.DriverId = request.DriverId;
        tour.DepartureAt = departureAt;
        tour.ReturnAt = returnAt;
        tour.Capacity = request.Capacity;
        tour.AvailableSeats = request.Capacity - bookedSeats;
        tour.MaxSeatsPerBooking = request.MaxSeatsPerBooking;
        tour.Price = request.Price;

        try
        {
            if (routeChanged)
            {
                // Two saves, so the old route rows are gone before the new ones are inserted
                // (the same point keeps its key, and positions may swap under the unique (TourId, Order) index).
                // The transaction makes the in-between state, a tour with no route, invisible to everyone else.
                await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    tour.PickupPoints.Clear();
                    await _unitOfWork.SaveChangesAsync(ct);
                    AddRoute(tour, request.PickupPointIds);
                    await _unitOfWork.SaveChangesAsync(ct);
                }, ct);
            }
            else
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }
        catch (ConcurrencyConflictException)
        {
            // Someone else, for example a customer booking a seat, saved the tour after we read it.
            return Result.Conflict(ChangedByOthers);
        }
        catch (ForeignKeyConstraintException)
        {
            // A booking arrived on the old route between the check above and the save.
            return Result.Conflict(LockedByBookings);
        }

        _logger.LogInformation("Tour {TourId} updated", id);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var tour = await _tours.GetForUpdateAsync(id, ct);
        if (tour is null)
        {
            return Result.NotFound(NotFoundMessage(id));
        }

        const string hasBookings = "The tour has bookings and cannot be deleted. Cancel it instead.";
        if (await _tours.HasBookingsAsync(id, ct))
        {
            return Result.Conflict(hasBookings);
        }

        _tours.Remove(tour);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Conflict(ChangedByOthers);
        }
        catch (ForeignKeyConstraintException)
        {
            return Result.Conflict(hasBookings);
        }

        _logger.LogInformation("Tour {TourId} deleted", id);
        return Result.Success();
    }

    /// <summary>
    /// Rules shared by create and update that the Data Annotations cannot express.
    /// Returns the first error, or the dates converted to UTC.
    /// </summary>
    private async Task<(string? Error, DateTime DepartureAt, DateTime ReturnAt)> ValidateAsync(TourRequest request,
        CancellationToken ct)
    {
        if (!UtcDates.TryToUtc(request.DepartureAt!.Value, out var departureAt) ||
            !UtcDates.TryToUtc(request.ReturnAt!.Value, out var returnAt))
        {
            return ("Dates must be in UTC (ending with Z) or include an offset, for example 2026-10-25T07:00:00Z.", default, default);
        }

        if (returnAt <= departureAt)
        {
            return ("The return time must be after the departure time.", default, default);
        }

        if (request.MaxSeatsPerBooking > request.Capacity)
        {
            return ("Seats per booking cannot exceed the capacity.", default, default);
        }

        if (request.PickupPointIds.Distinct().Count() != request.PickupPointIds.Count)
        {
            return ("A pickup point can appear only once in the route.", default, default);
        }

        if (await _destinations.GetByIdAsync(request.DestinationId, ct) is null)
        {
            // 400, not 404: the URL is fine, the body points at something that does not exist.
            return ($"Destination {request.DestinationId} does not exist.", default, default);
        }

        if (request.DriverId is { } driverId)
        {
            var driver = await _users.GetByIdAsync(driverId, ct);
            if (driver is null || driver.Role != UserRole.Driver)
            {
                return ($"User {driverId} is not a driver.", default, default);
            }
        }

        if (await _pickupPoints.CountExistingAsync(request.PickupPointIds, ct) != request.PickupPointIds.Count)
        {
            return ("One or more pickup points do not exist.", default, default);
        }

        return (null, departureAt, returnAt);
    }

    private static void AddRoute(Tour tour, IReadOnlyList<int> pickupPointIds)
    {
        for (var i = 0; i < pickupPointIds.Count; i++)
        {
            tour.PickupPoints.Add(new TourPickupPoint { PickupPointId = pickupPointIds[i], Order = i + 1 });
        }
    }

    private DateTime UtcNow() => _time.GetUtcNow().UtcDateTime;

    private static string NotFoundMessage(int id) => $"Tour {id} was not found.";
}
