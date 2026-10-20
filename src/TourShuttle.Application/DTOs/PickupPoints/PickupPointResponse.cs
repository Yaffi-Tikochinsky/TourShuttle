namespace TourShuttle.Application.DTOs.PickupPoints;

public class PickupPointResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string City { get; set; } = default!;
}
