using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.Application.DTOs.PickupPoints;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Controllers;

/// <summary>Pickup points. Signed-in users can read them; only an admin can change them.</summary>
[Route("api/pickup-points")]
[Authorize]
public class PickupPointsController : ApiControllerBase
{
    private readonly IPickupPointService _pickupPoints;

    public PickupPointsController(IPickupPointService pickupPoints)
    {
        _pickupPoints = pickupPoints;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PickupPointResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PickupPointResponse>>> List(CancellationToken ct) =>
        Ok(await _pickupPoints.ListAsync(ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<PickupPointResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PickupPointResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _pickupPoints.GetByIdAsync(id, ct));

    // [Authorize] on the class and [Authorize(Roles)] here both apply: signed in AND an admin.
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType<PickupPointResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PickupPointResponse>> Create([FromBody] PickupPointRequest request, CancellationToken ct)
    {
        var result = await _pickupPoints.CreateAsync(request, ct);
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
    public async Task<ActionResult> Update([FromRoute] int id, [FromBody] PickupPointRequest request, CancellationToken ct) =>
        FromResult(await _pickupPoints.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Delete([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _pickupPoints.DeleteAsync(id, ct));
}
