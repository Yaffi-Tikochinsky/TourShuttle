namespace TourShuttle.Application.Interfaces;

public interface IPasswordHasher
{
    /// <summary>Returns a salted, slow hash that is safe to store.</summary>
    string Hash(string password);

    /// <summary>Checks a login attempt against a stored hash.</summary>
    bool Verify(string passwordHash, string password);
}
