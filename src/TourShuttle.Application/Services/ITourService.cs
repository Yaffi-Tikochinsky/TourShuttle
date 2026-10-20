using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Services;

public interface ITourService
{
    Task<Result<PagedResult<TourSummaryResponse>>> ListAsync(TourQueryParameters query, bool isAdmin, CancellationToken ct);
    Task<Result<TourDetailsResponse>> GetByIdAsync(int id, bool isAdmin, CancellationToken ct);
    Task<Result<TourDetailsResponse>> CreateAsync(TourRequest request, CancellationToken ct);

    /// <summary>
    /// Price, driver, seats per booking, return time and capacity can always change
    /// (capacity never below the seats already booked). Destination, departure and route
    /// are locked once the tour has bookings.
    /// </summary>
    Task<Result> UpdateAsync(int id, TourRequest request, CancellationToken ct);

    /// <summary>Refused with Conflict when the tour has bookings.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken ct);

    /// <summary>Active passengers grouped by pickup point. An admin, or the driver assigned to this tour.</summary>
    Task<Result<TourPassengersResponse>> GetPassengersAsync(int id, int requesterId, UserRole requesterRole,
        CancellationToken ct);

    /// <summary>The driver's tours that have not returned yet.</summary>
    Task<IReadOnlyList<TourSummaryResponse>> ListForDriverAsync(int driverId, CancellationToken ct);
}
