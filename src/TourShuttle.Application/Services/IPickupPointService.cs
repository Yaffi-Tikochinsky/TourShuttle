using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.PickupPoints;

namespace TourShuttle.Application.Services;

public interface IPickupPointService
{
    Task<IReadOnlyList<PickupPointResponse>> ListAsync(CancellationToken ct);
    Task<Result<PickupPointResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<PickupPointResponse>> CreateAsync(PickupPointRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, PickupPointRequest request, CancellationToken ct);

    /// <summary>Refused with Conflict while any tour's route uses the point.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
