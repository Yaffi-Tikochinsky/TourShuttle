namespace TourShuttle.Application.DTOs.Tours;

/// <summary>What the client shows. Only Scheduled and Cancelled are stored; the rest come from the clock.</summary>
public enum TourDisplayStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public enum TourSortBy
{
    Departure,
    Price
}

public enum SortDirection
{
    Asc,
    Desc
}
