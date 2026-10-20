using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.DTOs.Tours;

/// <summary>One row of the tours list.</summary>
public class TourSummaryResponse
{
    public int Id { get; set; }
    public int DestinationId { get; set; }
    public string DestinationName { get; set; } = default!;
    public string City { get; set; } = default!;
    public DateTime DepartureAt { get; set; }
    public DateTime ReturnAt { get; set; }
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int AvailableSeats { get; set; }
    public int MaxSeatsPerBooking { get; set; }
    public TourStatus Status { get; set; }
    public TourDisplayStatus DisplayStatus { get; set; }

    /// <summary>The name only; the driver's phone is not public.</summary>
    public string? DriverName { get; set; }
}
