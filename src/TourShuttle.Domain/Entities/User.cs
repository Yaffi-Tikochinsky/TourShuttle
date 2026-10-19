using TourShuttle.Domain.Enums;

namespace TourShuttle.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public UserRole Role { get; set; }

    /// <summary>Bookings made by this user as a customer.</summary>
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    /// <summary>Tours this user is assigned to as a driver.</summary>
    public ICollection<Tour> DrivenTours { get; set; } = new List<Tour>();
}
