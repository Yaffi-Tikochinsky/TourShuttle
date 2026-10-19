using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.Application.DTOs.Auth;
using TourShuttle.Application.Services;

namespace TourShuttle.API.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Creates a customer account and returns a token, so the new user is already signed in.</summary>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        // [ApiController] already returned 400 if the Data Annotations on the DTO failed (ModelState).
        var result = await _auth.RegisterAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = result.Value!.User.Id }, result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        FromResult(await _auth.LoginAsync(request, ct));
}
