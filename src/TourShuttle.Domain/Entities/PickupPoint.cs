namespace TourShuttle.Domain.Entities;

/// <summary>A place where shuttles pick up passengers. One point can serve many tours.</summary>
public class PickupPoint
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;

    public ICollection<TourPickupPoint> Tours { get; set; } = new List<TourPickupPoint>();
}
