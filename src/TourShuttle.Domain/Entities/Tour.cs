using TourShuttle.Domain.Enums;

namespace TourShuttle.Domain.Entities;

/// <summary>
/// A shuttle leaving at a set time to a destination. Its seats are the limited resource
/// that customers compete for, guarded by the <see cref="Version"/> concurrency token.
/// </summary>
public class Tour
{
    public int Id { get; set; }

    public int DestinationId { get; set; }
    public Destination Destination { get; set; } = default!;

    public int? DriverId { get; set; }
    public User? Driver { get; set; }

    /// <summary>Pickup time at the first point (UTC).</summary>
    public DateTime DepartureAt { get; set; }

    /// <summary>Expected return time (UTC).</summary>
    public DateTime ReturnAt { get; set; }

    public int Capacity { get; set; }
    public int AvailableSeats { get; set; }
    public int MaxSeatsPerBooking { get; set; }
    public decimal Price { get; set; }
    /// <summary>
    /// Minutes between consecutive pickup points for this tour only (for example, a route through heavy traffic).
    /// Null means "use the departure-hour band"; when set, it wins over the band.
    /// </summary>
    public int? MinutesBetweenPickupPoints { get; set; }

    public TourStatus Status { get; set; } = TourStatus.Scheduled;

    /// <summary>Renewed on every update; a stale value makes the update fail with a conflict.</summary>
    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<TourPickupPoint> PickupPoints { get; set; } = new List<TourPickupPoint>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
