namespace TourShuttle.Domain.Entities;

/// <summary>A place where shuttles pick up passengers. One point can serve many tours.</summary>
public class PickupPoint
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;

    /// <summary>
    /// The city the stop is in. Customers filter tours by it: a tour appears for a city
    /// when any stop on its route is there, since a route can pass through several cities.
    /// </summary>
    public string City { get; set; } = default!;

    public ICollection<TourPickupPoint> Tours { get; set; } = new List<TourPickupPoint>();
}
