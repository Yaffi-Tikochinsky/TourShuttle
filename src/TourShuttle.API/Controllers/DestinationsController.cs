using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.Application.DTOs.Destinations;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Controllers;

/// <summary>Tour sites. Anyone can browse them; only an admin can change them.</summary>
[Route("api/destinations")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class DestinationsController : ApiControllerBase
{
    private readonly IDestinationService _destinations;

    public DestinationsController(IDestinationService destinations)
    {
        _destinations = destinations;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<DestinationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DestinationResponse>>> List(CancellationToken ct) =>
        Ok(await _destinations.ListAsync(ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType<DestinationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DestinationResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _destinations.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType<DestinationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DestinationResponse>> Create([FromBody] DestinationRequest request, CancellationToken ct)
    {
        var result = await _destinations.CreateAsync(request, ct);
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
    public async Task<ActionResult> Update([FromRoute] int id, [FromBody] DestinationRequest request, CancellationToken ct) =>
        FromResult(await _destinations.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _destinations.DeleteAsync(id, ct));
}
