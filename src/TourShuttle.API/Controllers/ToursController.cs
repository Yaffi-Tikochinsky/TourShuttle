using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Controllers;

/// <summary>Shuttle tours. Anyone can browse them; only an admin can change them.</summary>
[Route("api/tours")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class ToursController : ApiControllerBase
{
    private readonly ITourService _tours;

    public ToursController(ITourService tours)
    {
        _tours = tours;
    }

    // [AllowAnonymous] still reads a token when one is sent, so an admin gets the admin view here too.
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

    [HttpPost]
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Update([FromRoute] int id, [FromBody] TourRequest request, CancellationToken ct) =>
        FromResult(await _tours.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _tours.DeleteAsync(id, ct));
}
