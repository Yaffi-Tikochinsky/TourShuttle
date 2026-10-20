using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Auth;
using TourShuttle.Application.DTOs.Users;

namespace TourShuttle.Application.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);

    /// <summary>An admin creates a driver account. Same fields and rules as registration; no token is issued.</summary>
    Task<Result<UserResponse>> CreateDriverAsync(RegisterRequest request, CancellationToken ct);
}
