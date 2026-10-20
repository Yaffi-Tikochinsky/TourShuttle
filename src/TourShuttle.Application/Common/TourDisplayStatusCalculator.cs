using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Domain.Enums;

namespace TourShuttle.Application.Common;

/// <summary>
/// "In progress" and "completed" are never stored: they follow from the clock,
/// so there is no background job that has to keep a status column up to date.
/// </summary>
public static class TourDisplayStatusCalculator
{
    public static TourDisplayStatus Calculate(TourStatus status, DateTime departureAt, DateTime returnAt, DateTime nowUtc)
    {
        if (status == TourStatus.Cancelled)
        {
            return TourDisplayStatus.Cancelled;
        }

        if (nowUtc < departureAt)
        {
            return TourDisplayStatus.Scheduled;
        }

        return nowUtc < returnAt ? TourDisplayStatus.InProgress : TourDisplayStatus.Completed;
    }
}
