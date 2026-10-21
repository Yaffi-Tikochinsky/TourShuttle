namespace TourShuttle.Application.DTOs.Tours;

/// <summary>The answer to cancelling a tour: how many active bookings were cancelled with it.</summary>
public class TourCancelledResponse
{
    public int TourId { get; set; }
    public int CancelledBookings { get; set; }
}
