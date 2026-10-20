using System.ComponentModel.DataAnnotations;

namespace TourShuttle.Application.DTOs.Tours;

/// <summary>Query string of GET /api/tours. Every parameter is optional.</summary>
public class TourQueryParameters
{
    public int? DestinationId { get; set; }

    /// <summary>Only tours with at least one stop in this city (see GET /api/pickup-points/cities).</summary>
    [StringLength(100)]
    public string? City { get; set; }

    /// <summary>Departures from this moment on (inclusive). Default: now, so past tours are hidden.</summary>
    public DateTime? From { get; set; }

    /// <summary>Departures before this moment (exclusive).</summary>
    public DateTime? To { get; set; }

    public TourSortBy SortBy { get; set; } = TourSortBy.Departure;

    public SortDirection SortDirection { get; set; } = SortDirection.Asc;

    [Range(1, 10_000)]
    public int Page { get; set; } = 1;

    /// <summary>Capped so no one can ask for the whole table in one request.</summary>
    [Range(1, 50)]
    public int PageSize { get; set; } = 10;

    /// <summary>Admins only; ignored for everyone else.</summary>
    public bool IncludeCancelled { get; set; }
}
