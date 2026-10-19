namespace TourShuttle.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many link between tours and pickup points.
/// <see cref="Order"/> is the position of the point along the route (1 = first pickup).
/// </summary>
public class TourPickupPoint
{
    public int TourId { get; set; }
    public Tour Tour { get; set; } = default!;

    public int PickupPointId { get; set; }
    public PickupPoint PickupPoint { get; set; } = default!;

    public int Order { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
