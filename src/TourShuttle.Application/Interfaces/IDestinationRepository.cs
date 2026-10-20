using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Interfaces;

public interface IDestinationRepository
{
    /// <summary>Read-only list, ordered by name.</summary>
    Task<IReadOnlyList<Destination>> ListAsync(CancellationToken ct);

    /// <summary>Read-only lookup (not tracked).</summary>
    Task<Destination?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Tracked lookup: changes to the result are saved by <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task<Destination?> GetForUpdateAsync(int id, CancellationToken ct);

    Task<bool> HasToursAsync(int id, CancellationToken ct);
    /// <summary>True when another destination (not <paramref name="exceptId"/>) already has this name.</summary>
    Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct);

    void Add(Destination destination);

    void Remove(Destination destination);
}
