namespace TourShuttle.Application.Common;

/// <summary>Turns incoming dates into UTC, or tells the caller the zone is unknown.</summary>
public static class UtcDates
{
    /// <summary>
    /// "2026-10-25T07:00:00Z" arrives as UTC and "...+03:00" as local time; both are exact.
    /// A date without Z or an offset is ambiguous, so it is refused instead of guessed.
    /// </summary>
    public static bool TryToUtc(DateTime value, out DateTime utc)
    {
        utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => default
        };
        return value.Kind != DateTimeKind.Unspecified;
    }

    /// <summary>For query filters, where a bare date is common: read it as UTC.</summary>
    public static DateTime? ToUtcLenient(DateTime? value) => value?.Kind switch
    {
        null => null,
        DateTimeKind.Local => value.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value!.Value, DateTimeKind.Utc)
    };
}
