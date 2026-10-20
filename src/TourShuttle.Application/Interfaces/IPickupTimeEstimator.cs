namespace TourShuttle.Application.Interfaces;

/// <summary>Estimates when a shuttle reaches each pickup point. Computed on every read, never stored.</summary>
public interface IPickupTimeEstimator
{
    /// <summary>
    /// Minutes between consecutive pickup points: the tour's own value when set,
    /// otherwise the band of the departure hour (Israel time), otherwise the default.
    /// </summary>
    int GetMinutesBetweenPoints(DateTime departureAtUtc, int? tourMinutes);

    /// <summary>DepartureAt + (order − 1) × minutes between points. Order 1 is the first pickup.</summary>
    DateTime EstimatePickupTime(DateTime departureAtUtc, int order, int? tourMinutes);
}
