using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Interfaces;

public interface IBookingRepository
{
    /// <summary>Read-only booking with its tour, destination and pickup point, in one query.</summary>
    Task<Booking?> GetDetailsAsync(int id, CancellationToken ct);

    /// <summary>Tracked booking with its tracked tour, so a cancellation can return the seats in the same save.</summary>
    Task<Booking?> GetForUpdateAsync(int id, CancellationToken ct);

    /// <summary>A customer's bookings with the same related data as <see cref="GetDetailsAsync"/>, newest tour first.</summary>
    Task<IReadOnlyList<Booking>> ListForUserAsync(int userId, CancellationToken ct);

    Task<bool> HasActiveBookingAsync(int tourId, int userId, CancellationToken ct);

    void Add(Booking booking);
}
