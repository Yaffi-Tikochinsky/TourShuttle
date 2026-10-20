using System.ComponentModel.DataAnnotations;

namespace TourShuttle.Application.DTOs.Bookings;

/// <summary>Body of POST /api/tours/{tourId}/bookings. The tour comes from the URL, the customer from the token.</summary>
public class BookingRequest
{
    [Range(1, int.MaxValue)]
    public int PickupPointId { get; set; }

    [Range(1, 20)]
    public int SeatsCount { get; set; }
}
