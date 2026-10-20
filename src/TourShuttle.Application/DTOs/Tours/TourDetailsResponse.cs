namespace TourShuttle.Application.DTOs.Tours;

/// <summary>A single tour with its route and estimated pickup times.</summary>
public class TourDetailsResponse : TourSummaryResponse
{
    public string? DestinationDescription { get; set; }

    /// <summary>Filled for admins only, so they can reassign the tour.</summary>
    public int? DriverId { get; set; }

    /// <summary>The value the estimated times below were computed with.</summary>
    public int MinutesBetweenPickupPoints { get; set; }

    public List<TourPickupPointResponse> PickupPoints { get; set; } = [];
}

public class TourPickupPointResponse
{
    public int PickupPointId { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public int Order { get; set; }

    /// <summary>Computed on every read, never stored. The client shows it as "estimated".</summary>
    public DateTime EstimatedPickupTimeUtc { get; set; }
}
