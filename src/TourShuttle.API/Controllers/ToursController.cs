using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.API.Extensions;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Controllers;

/// <summary>
/// Shuttle tours. Anyone can browse them; only an admin can change them.
/// Roles are set per action: a class-level [Authorize(Roles = Admin)] would also apply to the
/// passenger list and block drivers, because class and action requirements must both pass.
/// </summary>
[Route("api/tours")]
public class ToursController : ApiControllerBase
{
    private const string AdminOrDriver = nameof(UserRole.Admin) + "," + nameof(UserRole.Driver);

    private readonly ITourService _tours;

    public ToursController(ITourService tours)
    {
        _tours = tours;
    }

    // Anonymous actions still read a token when one is sent, so an admin gets the admin view there too.
    private bool IsAdmin => User.IsInRole(nameof(UserRole.Admin));

    /// <summary>Upcoming tours, filtered, sorted and paged in the database.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedResult<TourSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TourSummaryResponse>>> List([FromQuery] TourQueryParameters query,
        CancellationToken ct) =>
        FromResult(await _tours.ListAsync(query, IsAdmin, ct));

    /// <summary>A tour with its route and the estimated pickup time at each point.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<TourDetailsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TourDetailsResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _tours.GetByIdAsync(id, IsAdmin, ct));

    /// <summary>Active passengers by pickup point: an admin, or the driver assigned to this tour.</summary>
    [HttpGet("{id:int}/passengers")]
    [Authorize(Roles = AdminOrDriver)]
    [ProducesResponseType<TourPassengersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TourPassengersResponse>> GetPassengers([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _tours.GetPassengersAsync(id, User.GetUserId(), User.GetRole(), ct));

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<TourDetailsResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TourDetailsResponse>> Create([FromBody] TourRequest request, CancellationToken ct)
    {
        var result = await _tours.CreateAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update([FromRoute] int id, [FromBody] TourRequest request, CancellationToken ct) =>
        FromResult(await _tours.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _tours.DeleteAsync(id, ct));

    /// <summary>
    /// Cancels a tour that has not left: the tour becomes Cancelled and all its active bookings are cancelled
    /// with it, in one transaction. The tour stays in the system (unlike DELETE), with its history.
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<TourCancelledResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TourCancelledResponse>> Cancel([FromRoute] int id, CancellationToken ct)
    {
        var result = await _tours.CancelAsync(id, ct);
        return result.IsSuccess
            ? Ok(new TourCancelledResponse { TourId = id, CancelledBookings = result.Value })
            : Failure(result);
    }
}
