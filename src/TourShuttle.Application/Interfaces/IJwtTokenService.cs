using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Interfaces;

public interface IJwtTokenService
{
    /// <summary>Creates a signed token whose claims identify the user and their role.</summary>
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}
