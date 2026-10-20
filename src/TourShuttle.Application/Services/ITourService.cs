using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Tours;

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
}
