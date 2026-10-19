using Microsoft.AspNetCore.Identity;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Infrastructure.Security;

/// <summary>
/// Adapter over ASP.NET Core Identity's hasher (PBKDF2 with a random salt and many iterations,
/// all stored in one string). Application only sees <see cref="Application.Interfaces.IPasswordHasher"/>.
/// </summary>
public class PasswordHasher : Application.Interfaces.IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    // The hasher's algorithm does not use the user object, so a placeholder is enough.
    private static readonly User Placeholder = new();

    public string Hash(string password) => _inner.HashPassword(Placeholder, password);

    public bool Verify(string passwordHash, string password) =>
        _inner.VerifyHashedPassword(Placeholder, passwordHash, password) != PasswordVerificationResult.Failed;
}
