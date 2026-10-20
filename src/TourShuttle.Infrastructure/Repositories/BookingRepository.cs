using Microsoft.EntityFrameworkCore;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;
using TourShuttle.Infrastructure.Persistence;

namespace TourShuttle.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _db;

    public BookingRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Booking?> GetDetailsAsync(int id, CancellationToken ct) =>
        WithDetails().FirstOrDefaultAsync(b => b.Id == id, ct);

    public Task<Booking?> GetForUpdateAsync(int id, CancellationToken ct) =>
        _db.Bookings.Include(b => b.Tour).FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Booking>> ListForUserAsync(int userId, CancellationToken ct) =>
        await WithDetails()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Tour.DepartureAt)
            .ToListAsync(ct);

    public Task<bool> HasActiveBookingAsync(int tourId, int userId, CancellationToken ct) =>
        _db.Bookings.AnyAsync(b => b.TourId == tourId && b.UserId == userId && b.Status == BookingStatus.Active, ct);

    public void Add(Booking booking) => _db.Bookings.Add(booking);

    // Only reference navigations (one row each), so the JOINs never multiply rows.
    private IQueryable<Booking> WithDetails() =>
        _db.Bookings.AsNoTracking()
            .Include(b => b.Tour).ThenInclude(t => t.Destination)
            .Include(b => b.TourPickupPoint).ThenInclude(tp => tp.PickupPoint);
}
