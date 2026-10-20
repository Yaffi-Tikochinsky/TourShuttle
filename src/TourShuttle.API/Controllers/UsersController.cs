using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.API.Extensions;
using TourShuttle.Application.DTOs.Auth;
using TourShuttle.Application.DTOs.Users;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Controllers;

[Route("api/users")]
[Authorize]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _users;
    private readonly IAuthService _auth;

    public UsersController(IUserService users, IAuthService auth)
    {
        _users = users;
        _auth = auth;
    }

    /// <summary>The profile of the signed-in user.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetMe(CancellationToken ct) =>
        FromResult(await _users.GetByIdAsync(User.GetUserId(), User.GetUserId(), User.GetRole(), ct));

    /// <summary>A user's profile: the user themselves or an admin.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _users.GetByIdAsync(id, User.GetUserId(), User.GetRole(), ct));

    /// <summary>All users, optionally filtered by role (for example, drivers to assign to a tour).</summary>
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List([FromQuery] UserRole? role, CancellationToken ct) =>
        Ok(await _users.ListAsync(role, ct));

    /// <summary>An admin adds a driver: the same fields as registration. 409 when the email is taken.</summary>
    [HttpPost("drivers")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateDriver([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _auth.CreateDriverAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }
}
