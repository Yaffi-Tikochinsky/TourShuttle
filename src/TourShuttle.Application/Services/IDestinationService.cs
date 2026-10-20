using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Destinations;

namespace TourShuttle.Application.Services;

public interface IDestinationService
{
    Task<IReadOnlyList<DestinationResponse>> ListAsync(CancellationToken ct);
    Task<Result<DestinationResponse>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<DestinationResponse>> CreateAsync(DestinationRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, DestinationRequest request, CancellationToken ct);

    /// <summary>Refused with Conflict while the destination has tours.</summary>
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
