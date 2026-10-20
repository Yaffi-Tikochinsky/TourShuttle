using System.ComponentModel.DataAnnotations;

namespace TourShuttle.Application.DTOs.PickupPoints;

/// <summary>Body of create and update (PUT replaces every field).</summary>
public class PickupPointRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Address { get; set; } = default!;
}
