using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.API.Extensions;
using TourShuttle.Application.DTOs.Bookings;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Controllers;

/// <summary>
/// Bookings. The routes follow the resources: a booking is created under its tour,
/// read and cancelled by its own id, and listed under the signed-in user.
/// </summary>
[Authorize]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookings;

    public BookingsController(IBookingService bookings)
    {
        _bookings = bookings;
    }

    /// <summary>Takes seats on a tour (the limited resource). 409 when the seats are gone.</summary>
    [HttpPost("api/tours/{tourId:int}/bookings")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Create([FromRoute] int tourId, [FromBody] BookingRequest request,
        CancellationToken ct)
    {
        var result = await _bookings.CreateAsync(tourId, User.GetUserId(), request, ct);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>A booking: its owner or an admin.</summary>
    [HttpGet("api/bookings/{id:int}")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> GetById([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _bookings.GetByIdAsync(id, User.GetUserId(), User.GetRole(), ct));

    /// <summary>Cancels a booking and returns its seats: its owner or an admin, until the tour leaves.</summary>
    [HttpDelete("api/bookings/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cancel([FromRoute] int id, CancellationToken ct) =>
        FromResult(await _bookings.CancelAsync(id, User.GetUserId(), User.GetRole(), ct));

    /// <summary>The signed-in customer's bookings, active and cancelled.</summary>
    [HttpGet("api/users/me/bookings")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType<IReadOnlyList<BookingResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> ListMine(CancellationToken ct) =>
        Ok(await _bookings.ListForCustomerAsync(User.GetUserId(), ct));
}
