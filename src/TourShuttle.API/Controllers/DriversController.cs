using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourShuttle.API.Extensions;
using TourShuttle.Application.DTOs.Tours;
using TourShuttle.Application.Services;
using TourShuttle.Domain.Enums;

namespace TourShuttle.API.Controllers;

[Route("api/drivers")]
[Authorize(Roles = nameof(UserRole.Driver))]
public class DriversController : ApiControllerBase
{
    private readonly ITourService _tours;

    public DriversController(ITourService tours)
    {
        _tours = tours;
    }

    /// <summary>The signed-in driver's tours that have not returned yet. Passengers: GET /api/tours/{id}/passengers.</summary>
    [HttpGet("me/tours")]
    [ProducesResponseType<IReadOnlyList<TourSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TourSummaryResponse>>> MyTours(CancellationToken ct) =>
        Ok(await _tours.ListForDriverAsync(User.GetUserId(), ct));
}
