using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Auth;

namespace TourShuttle.Application.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}
