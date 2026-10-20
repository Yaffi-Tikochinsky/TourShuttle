using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.DTOs.Bookings;

/// <summary>A booking as its customer sees it: what, when, where to wait and how much.</summary>
public class BookingResponse
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public string DestinationName { get; set; } = default!;
    public DateTime DepartureAt { get; set; }
    public DateTime ReturnAt { get; set; }
    public TourDisplayStatus TourDisplayStatus { get; set; }
    public int PickupPointId { get; set; }
    public string PickupPointName { get; set; } = default!;
    public string PickupPointAddress { get; set; } = default!;
    public DateTime EstimatedPickupTimeUtc { get; set; }
    public int SeatsCount { get; set; }

    /// <summary>Price per seat times seats, fixed when the booking was made.</summary>
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
    public BookingStatus Status { get; set; }
}
