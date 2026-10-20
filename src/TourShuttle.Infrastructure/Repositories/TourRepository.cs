using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;
using TourShuttle.Infrastructure.Persistence;

namespace TourShuttle.Infrastructure.Repositories;

public class TourRepository : ITourRepository
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public TourRepository(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<(IReadOnlyList<TourSummaryResponse> Items, int TotalCount)> ListAsync(
        TourListFilter filter, CancellationToken ct)
    {
        // Still an IQueryable: every Where below adds to the SQL; nothing runs until CountAsync/ToListAsync.
        var query = _db.Tours.AsNoTracking().Where(t => t.DepartureAt >= filter.From);

        if (filter.To is { } to)
        {
            query = query.Where(t => t.DepartureAt < to);
        }

        if (filter.DestinationId is { } destinationId)
        {
            query = query.Where(t => t.DestinationId == destinationId);
        }

        if (!filter.IncludeCancelled)
        {
            query = query.Where(t => t.Status != TourStatus.Cancelled);
        }

        var totalCount = await query.CountAsync(ct);

        var ordered = filter.SortBy switch
        {
            TourSortBy.Price => filter.Descending ? query.OrderByDescending(t => t.Price) : query.OrderBy(t => t.Price),
            _ => filter.Descending ? query.OrderByDescending(t => t.DepartureAt) : query.OrderBy(t => t.DepartureAt)
        };

        // Id as a tie-breaker keeps the order stable, so a tour never shows up on two pages.
        var items = await ordered.ThenBy(t => t.Id)
            .Skip(filter.Skip)
            .Take(filter.Take)
            // Builds the DTO inside the SELECT: one query with JOINs to Destinations and Users, no N+1.
            .ProjectTo<TourSummaryResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Tour?> GetDetailsAsync(int id, CancellationToken ct) =>
        _db.Tours.AsNoTracking()
            .Include(t => t.Destination)
            .Include(t => t.Driver)
            .Include(t => t.PickupPoints).ThenInclude(tp => tp.PickupPoint)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tour?> GetForUpdateAsync(int id, CancellationToken ct) =>
        _db.Tours
            .Include(t => t.PickupPoints)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<bool> HasBookingsAsync(int id, CancellationToken ct) =>
        _db.Bookings.AnyAsync(b => b.TourId == id, ct);

    public void Add(Tour tour) => _db.Tours.Add(tour);

    public void Remove(Tour tour) => _db.Tours.Remove(tour);
}
