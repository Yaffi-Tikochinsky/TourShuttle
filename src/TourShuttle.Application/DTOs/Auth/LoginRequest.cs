using System.ComponentModel.DataAnnotations;

namespace TourShuttle.Application.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = default!;

    // The same limit as registration: no one has a longer password, and huge inputs are refused before hashing.
    [Required]
    [StringLength(100)]
    public string Password { get; set; } = default!;
}
