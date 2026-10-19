using System.ComponentModel.DataAnnotations;

namespace TourShuttle.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [StringLength(200)]
    public string Email { get; set; } = default!;

    [Required]
    [Phone]
    [StringLength(20, MinimumLength = 9)]
    public string Phone { get; set; } = default!;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "The password must contain at least one letter and one digit.")]
    public string Password { get; set; } = default!;
}
