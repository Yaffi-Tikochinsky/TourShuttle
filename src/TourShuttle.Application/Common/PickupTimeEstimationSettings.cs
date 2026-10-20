namespace TourShuttle.Application.Common;

/// <summary>
/// Bound from the "PickupTimeEstimation" configuration section: how many minutes the shuttle needs
/// between two consecutive pickup points, depending on the hour it leaves (Israel time).
/// </summary>
public class PickupTimeEstimationSettings
{
    public const string SectionName = "PickupTimeEstimation";

    /// <summary>IANA time zone the bands are written in, such as "Asia/Jerusalem".</summary>
    public string TimeZone { get; set; } = "Asia/Jerusalem";

    /// <summary>Used when the departure time is in no band.</summary>
    public int DefaultMinutes { get; set; } = 2;

    public List<TimeBand> Bands { get; set; } = [];
}

/// <summary>
/// A range of local departure times. From is inclusive and To is exclusive.
/// When From is later than To (23:00 to 07:30) the band crosses midnight.
/// </summary>
public class TimeBand
{
    public TimeOnly From { get; set; }
    public TimeOnly To { get; set; }
    public int Minutes { get; set; }

    public bool Contains(TimeOnly time) =>
        From <= To
            ? time >= From && time < To
            // Crosses midnight: 23:30 is after From, 06:00 is before To.
            : time >= From || time < To;
}
