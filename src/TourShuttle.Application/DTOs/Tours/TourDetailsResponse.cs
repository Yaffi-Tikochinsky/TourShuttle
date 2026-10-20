namespace TourShuttle.Application.DTOs.Tours;

/// <summary>A single tour with its route and estimated pickup times.</summary>
public class TourDetailsResponse : TourSummaryResponse
{
    public string? DestinationDescription { get; set; }

    /// <summary>Filled for admins only, so they can reassign the tour.</summary>
    public int? DriverId { get; set; }

    /// <summary>The value the estimated times below were computed with (the tour's own value or the band's).</summary>
    public int MinutesBetweenPickupPoints { get; set; }

    /// <summary>
    /// The value stored on the tour, or null when the band decides. An edit form uses it as the input's value
    /// (and the field above as its placeholder), so a PUT that changes another field does not clear it.
    /// </summary>
    public int? CustomMinutesBetweenPickupPoints { get; set; }

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
