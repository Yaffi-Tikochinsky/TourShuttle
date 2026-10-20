using Microsoft.EntityFrameworkCore;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Infrastructure.Persistence;

namespace TourShuttle.Infrastructure.Repositories;

public class PickupPointRepository : IPickupPointRepository
{
    private readonly AppDbContext _db;

    public PickupPointRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PickupPoint>> ListAsync(CancellationToken ct) =>
        await _db.PickupPoints.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);

    public Task<PickupPoint?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PickupPoints.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<PickupPoint?> GetForUpdateAsync(int id, CancellationToken ct) =>
        _db.PickupPoints.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> IsUsedByToursAsync(int id, CancellationToken ct) =>
        _db.TourPickupPoints.AnyAsync(tp => tp.PickupPointId == id, ct);

    public void Add(PickupPoint pickupPoint) => _db.PickupPoints.Add(pickupPoint);

    public void Remove(PickupPoint pickupPoint) => _db.PickupPoints.Remove(pickupPoint);
}
