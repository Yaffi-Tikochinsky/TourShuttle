namespace TourShuttle.Domain.Entities;

/// <summary>A tour site the shuttles travel to, such as the Western Wall.</summary>
public class Destination
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string City { get; set; } = default!;

    public ICollection<Tour> Tours { get; set; } = new List<Tour>();
}
