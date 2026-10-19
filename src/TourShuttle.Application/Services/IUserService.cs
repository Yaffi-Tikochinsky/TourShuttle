using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Users;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Services;

public interface IUserService
{
    /// <summary>A user may read their own profile; an admin may read anyone's.</summary>
    Task<Result<UserResponse>> GetByIdAsync(int id, int requesterId, UserRole requesterRole, CancellationToken ct);

    Task<IReadOnlyList<UserResponse>> ListAsync(UserRole? role, CancellationToken ct);
}
