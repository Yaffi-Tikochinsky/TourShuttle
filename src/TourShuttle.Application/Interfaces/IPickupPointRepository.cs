using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Interfaces;

public interface IPickupPointRepository
{
    /// <summary>Read-only list, ordered by name.</summary>
    Task<IReadOnlyList<PickupPoint>> ListAsync(CancellationToken ct);

    /// <summary>Read-only lookup (not tracked).</summary>
    Task<PickupPoint?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Tracked lookup: changes to the result are saved by <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    Task<PickupPoint?> GetForUpdateAsync(int id, CancellationToken ct);

    /// <summary>How many of the given ids exist, in one query.</summary>
    Task<int> CountExistingAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    /// <summary>Every city that has at least one stop, alphabetically, each once.</summary>
    Task<IReadOnlyList<string>> ListCitiesAsync(CancellationToken ct);

    /// <summary>True when any tour's route includes the point.</summary>
    Task<bool> IsUsedByToursAsync(int id, CancellationToken ct);
    /// <summary>True when another pickup point (not <paramref name="exceptId"/>) already has this name.</summary>
    Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct);

    void Add(PickupPoint pickupPoint);

    void Remove(PickupPoint pickupPoint);
}
