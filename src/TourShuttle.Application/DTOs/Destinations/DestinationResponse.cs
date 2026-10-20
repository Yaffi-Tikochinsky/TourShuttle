namespace TourShuttle.Application.DTOs.Destinations;

public class DestinationResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string City { get; set; } = default!;
}
