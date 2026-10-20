namespace TourShuttle.Application.DTOs.Tours;

/// <summary>
/// The query parameters after the service resolved defaults and permissions,
/// in the form the repository turns into SQL (Skip/Take instead of page numbers).
/// </summary>
public record TourListFilter(
    int? DestinationId,
    string? City,
    DateTime From,
    DateTime? To,
    TourSortBy SortBy,
    bool Descending,
    bool IncludeCancelled,
    int Skip,
    int Take);
