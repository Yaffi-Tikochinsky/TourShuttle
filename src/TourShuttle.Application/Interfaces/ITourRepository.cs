using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Interfaces;

public interface ITourRepository
{
    /// <summary>
    /// One page of tours, filtered and sorted in SQL, plus the total count for the pager.
    /// Exactly two queries (COUNT and SELECT), whatever the page size.
    /// </summary>
    Task<(IReadOnlyList<TourSummaryResponse> Items, int TotalCount)> ListAsync(TourListFilter filter, CancellationToken ct);

    /// <summary>Read-only tour with destination, driver and route (with pickup points), in one query.</summary>
    Task<Tour?> GetDetailsAsync(int id, CancellationToken ct);

    /// <summary>Tracked tour with its route rows, for update and delete.</summary>
    Task<Tour?> GetForUpdateAsync(int id, CancellationToken ct);

    /// <summary>
    /// Read-only tour with its route (with pickup points) and its active bookings (with customers),
    /// in one query, for the passenger list.
    /// </summary>
    Task<Tour?> GetWithPassengersAsync(int id, CancellationToken ct);

    /// <summary>A driver's tours that have not returned yet, cancelled ones included, by departure.</summary>
    Task<IReadOnlyList<TourSummaryResponse>> ListForDriverAsync(int driverId, DateTime nowUtc, CancellationToken ct);

    /// <summary>True when the tour has any booking, active or cancelled.</summary>
    Task<bool> HasBookingsAsync(int id, CancellationToken ct);
    /// <summary>Tracked tour with its active bookings (tracked too), so cancelling the tour cancels them in the same save.</summary>
    Task<Tour?> GetWithActiveBookingsForUpdateAsync(int id, CancellationToken ct);

    void Add(Tour tour);

    void Remove(Tour tour);
}
