using TourShuttle.Domain.Entities;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Interfaces;

public interface IUserRepository
{
    /// <summary>Read-only lookup (not tracked).</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>Read-only lookup by normalized (lower-case) email.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    Task<IReadOnlyList<User>> ListAsync(UserRole? role, CancellationToken ct);

    /// <summary>Stages a new user; it is written on <see cref="IUnitOfWork.SaveChangesAsync"/>.</summary>
    void Add(User user);
}
