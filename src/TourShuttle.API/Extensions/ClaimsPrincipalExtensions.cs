using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Extensions;

/// <summary>Reads the identity of the caller from the validated JWT claims.</summary>
public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The token has no 'sub' claim."));

    public static UserRole GetRole(this ClaimsPrincipal user) =>
        Enum.Parse<UserRole>(user.FindFirstValue("role")
            ?? throw new InvalidOperationException("The token has no 'role' claim."));
}
