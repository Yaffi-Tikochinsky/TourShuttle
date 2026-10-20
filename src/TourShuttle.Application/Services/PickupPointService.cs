using AutoMapper;
using Microsoft.Extensions.Logging;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.PickupPoints;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Services;

public class PickupPointService : IPickupPointService
{
    private const string InUse = "The pickup point is on the route of at least one tour. Remove it from those tours first.";

    private readonly IPickupPointRepository _pickupPoints;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PickupPointService> _logger;

    public PickupPointService(IPickupPointRepository pickupPoints, IUnitOfWork unitOfWork, IMapper mapper,
        ILogger<PickupPointService> logger)
    {
        _pickupPoints = pickupPoints;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PickupPointResponse>> ListAsync(CancellationToken ct) =>
        _mapper.Map<IReadOnlyList<PickupPointResponse>>(await _pickupPoints.ListAsync(ct));

    public async Task<Result<PickupPointResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var pickupPoint = await _pickupPoints.GetByIdAsync(id, ct);
        return pickupPoint is null
            ? Result<PickupPointResponse>.NotFound(NotFoundMessage(id))
            : Result<PickupPointResponse>.Success(_mapper.Map<PickupPointResponse>(pickupPoint));
    }

    public async Task<Result<PickupPointResponse>> CreateAsync(PickupPointRequest request, CancellationToken ct)
    {
        var pickupPoint = _mapper.Map<PickupPoint>(request);
        _pickupPoints.Add(pickupPoint);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Pickup point {PickupPointId} created", pickupPoint.Id);
        return Result<PickupPointResponse>.Success(_mapper.Map<PickupPointResponse>(pickupPoint));
    }

    public async Task<Result> UpdateAsync(int id, PickupPointRequest request, CancellationToken ct)
    {
        var pickupPoint = await _pickupPoints.GetForUpdateAsync(id, ct);
        if (pickupPoint is null)
        {
            return Result.NotFound(NotFoundMessage(id));
        }

        _mapper.Map(request, pickupPoint);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Pickup point {PickupPointId} updated", id);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var pickupPoint = await _pickupPoints.GetForUpdateAsync(id, ct);
        if (pickupPoint is null)
        {
            return Result.NotFound(NotFoundMessage(id));
        }

        if (await _pickupPoints.IsUsedByToursAsync(id, ct))
        {
            return Result.Conflict(InUse);
        }

        _pickupPoints.Remove(pickupPoint);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ForeignKeyConstraintException)
        {
            // A tour started using the point between the check above and the delete.
            return Result.Conflict(InUse);
        }

        _logger.LogInformation("Pickup point {PickupPointId} deleted", id);
        return Result.Success();
    }

    private static string NotFoundMessage(int id) => $"Pickup point {id} was not found.";
}
