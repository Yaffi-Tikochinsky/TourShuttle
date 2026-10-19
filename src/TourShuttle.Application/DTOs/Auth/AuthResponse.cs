using TourShuttle.Application.DTOs.Users;

namespace TourShuttle.Application.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public UserResponse User { get; set; } = default!;
}
