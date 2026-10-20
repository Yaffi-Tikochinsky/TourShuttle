namespace TourShuttle.Application.DTOs.Tours;

/// <summary>The driver's pickup sheet: who to pick up at each stop, in route order.</summary>
public class TourPassengersResponse
{
    public int TourId { get; set; }
    public string DestinationName { get; set; } = default!;
    public DateTime DepartureAt { get; set; }
    public int TotalSeats { get; set; }
    public List<PickupPointPassengersResponse> PickupPoints { get; set; } = [];
}

public class PickupPointPassengersResponse
{
    public int PickupPointId { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public int Order { get; set; }
    public DateTime EstimatedPickupTimeUtc { get; set; }

    /// <summary>Seats to pick up at this stop.</summary>
    public int TotalSeats { get; set; }

    public List<PassengerResponse> Passengers { get; set; } = [];
}

public class PassengerResponse
{
    public int BookingId { get; set; }
    public string FullName { get; set; } = default!;

    /// <summary>Shown to the admin and the assigned driver only, so the driver can call a late passenger.</summary>
    public string Phone { get; set; } = default!;

    public int SeatsCount { get; set; }
}
