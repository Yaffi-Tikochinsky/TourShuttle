using System.ComponentModel.DataAnnotations;

namespace TourShuttle.Application.DTOs.Destinations;

/// <summary>Body of create and update (PUT replaces every field).</summary>
public class DestinationRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = default!;
}
