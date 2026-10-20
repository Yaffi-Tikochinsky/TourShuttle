using System.ComponentModel.DataAnnotations;

namespace TourShuttle.Application.DTOs.Tours;

/// <summary>
/// Body of create and update (PUT replaces every field). Rules that need the database
/// (the destination exists, the driver is a driver) are checked in TourService.
/// </summary>
public class TourRequest
{
    [Range(1, int.MaxValue)]
    public int DestinationId { get; set; }

    /// <summary>Optional: a tour can be published before a driver is assigned.</summary>
    public int? DriverId { get; set; }

    /// <summary>Pickup time at the first point. Must be UTC ("Z") or carry an offset.</summary>
    [Required]
    public DateTime? DepartureAt { get; set; }

    [Required]
    public DateTime? ReturnAt { get; set; }

    [Range(1, 200)]
    public int Capacity { get; set; }

    [Range(1, 20)]
    public int MaxSeatsPerBooking { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal Price { get; set; }

    /// <summary>
    /// Optional minutes between stops for this tour, overriding the departure-hour band.
    /// PUT replaces every field, so leaving it out clears a value set before: send back
    /// customMinutesBetweenPickupPoints from GET to keep it.
    /// </summary>
    [Range(1, 30)]
    public int? MinutesBetweenPickupPoints { get; set; }

    /// <summary>The route in order: the first id is picked up first.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(20)]
    public List<int> PickupPointIds { get; set; } = [];
}
