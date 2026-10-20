using Microsoft.EntityFrameworkCore;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Infrastructure.Persistence;

namespace TourShuttle.Infrastructure.Repositories;

public class DestinationRepository : IDestinationRepository
{
    private readonly AppDbContext _db;

    public DestinationRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Destination>> ListAsync(CancellationToken ct) =>
        await _db.Destinations.AsNoTracking().OrderBy(d => d.Name).ToListAsync(ct);

    public Task<Destination?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Destinations.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<Destination?> GetForUpdateAsync(int id, CancellationToken ct) =>
        _db.Destinations.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<bool> HasToursAsync(int id, CancellationToken ct) =>
        _db.Tours.AnyAsync(t => t.DestinationId == id, ct);

    public void Add(Destination destination) => _db.Destinations.Add(destination);

    public void Remove(Destination destination) => _db.Destinations.Remove(destination);
}
