namespace TourShuttle.Application.DTOs.Users;

/// <summary>What the API exposes about a user. Never includes the password hash.</summary>
public class UserResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Role { get; set; } = default!;
}
