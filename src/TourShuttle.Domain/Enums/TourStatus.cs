namespace TourShuttle.Domain.Enums;

/// <summary>
/// Only states that someone decides on are stored.
/// "In progress" and "completed" are derived from the departure and return times.
/// </summary>
public enum TourStatus
{
    Scheduled,
    Cancelled
}
