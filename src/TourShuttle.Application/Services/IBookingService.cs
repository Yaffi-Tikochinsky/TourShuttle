using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Bookings;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Services;

public interface IBookingService
{
    /// <summary>
    /// Takes seats on a tour. Conflict when the seats run out or the customer already has an active
    /// booking for the tour. A concurrency conflict is retried with fresh data before giving up.
    /// </summary>
    Task<Result<BookingResponse>> CreateAsync(int tourId, int customerId, BookingRequest request, CancellationToken ct);

    /// <summary>The owner or an admin.</summary>
    Task<Result<BookingResponse>> GetByIdAsync(int id, int requesterId, UserRole requesterRole, CancellationToken ct);

    Task<IReadOnlyList<BookingResponse>> ListForCustomerAsync(int customerId, CancellationToken ct);

    /// <summary>The owner or an admin, until the tour leaves. Returns the seats to the tour.</summary>
    Task<Result> CancelAsync(int id, int requesterId, UserRole requesterRole, CancellationToken ct);
}
