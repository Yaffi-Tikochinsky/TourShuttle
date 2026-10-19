using TourShuttle.Domain.Enums;

namespace TourShuttle.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public int TourId { get; set; }
    public Tour Tour { get; set; } = default!;

    public int UserId { get; set; }
    public User User { get; set; } = default!;

    /// <summary>Together with <see cref="TourId"/>, points at a <see cref="Entities.TourPickupPoint"/>,
    /// so the database guarantees the pickup point belongs to this tour.</summary>
    public int PickupPointId { get; set; }
    public TourPickupPoint TourPickupPoint { get; set; } = default!;

    public int SeatsCount { get; set; }

    /// <summary>Price per seat times seats, fixed at booking time.</summary>
    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Active;
}
