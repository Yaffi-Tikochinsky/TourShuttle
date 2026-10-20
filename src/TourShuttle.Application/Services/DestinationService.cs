using AutoMapper;
using Microsoft.Extensions.Logging;
using TourShuttle.Application.Common;
using TourShuttle.Application.DTOs.Destinations;
using TourShuttle.Application.Interfaces;
using TourShuttle.Domain.Entities;

namespace TourShuttle.Application.Services;

public class DestinationService : IDestinationService
{
    private const string HasTours = "The destination has tours. Delete its tours first.";

    private static string NameTaken(string name) => $"A destination named '{name}' already exists.";

    private readonly IDestinationRepository _destinations;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<DestinationService> _logger;

    public DestinationService(IDestinationRepository destinations, IUnitOfWork unitOfWork, IMapper mapper,
        ILogger<DestinationService> logger)
    {
        _destinations = destinations;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DestinationResponse>> ListAsync(CancellationToken ct) =>
        _mapper.Map<IReadOnlyList<DestinationResponse>>(await _destinations.ListAsync(ct));

    public async Task<Result<DestinationResponse>> GetByIdAsync(int id, CancellationToken ct)
    {
        var destination = await _destinations.GetByIdAsync(id, ct);
        return destination is null
            ? Result<DestinationResponse>.NotFound(NotFoundMessage(id))
            : Result<DestinationResponse>.Success(_mapper.Map<DestinationResponse>(destination));
    }

    public async Task<Result<DestinationResponse>> CreateAsync(DestinationRequest request, CancellationToken ct)
    {
        var destination = _mapper.Map<Destination>(request);
        Trim(destination);
        if (await _destinations.NameExistsAsync(destination.Name, null, ct))
        {
            return Result<DestinationResponse>.Conflict(NameTaken(destination.Name));
        }

        _destinations.Add(destination);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            // The same name was saved by another request between the check and the save.
            return Result<DestinationResponse>.Conflict(NameTaken(destination.Name));
        }

        _logger.LogInformation("Destination {DestinationId} created", destination.Id);
        return Result<DestinationResponse>.Success(_mapper.Map<DestinationResponse>(destination));
    }

    public async Task<Result> UpdateAsync(int id, DestinationRequest request, CancellationToken ct)
    {
        var destination = await _destinations.GetForUpdateAsync(id, ct);
        if (destination is null)
        {
            return Result.NotFound(NotFoundMessage(id));
        }

        // Copies the request onto the tracked entity; EF Core saves only the columns that changed.
        _mapper.Map(request, destination);
        Trim(destination);
        if (await _destinations.NameExistsAsync(destination.Name, id, ct))
        {
            return Result.Conflict(NameTaken(destination.Name));
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException)
        {
            return Result.Conflict(NameTaken(destination.Name));
        }

        _logger.LogInformation("Destination {DestinationId} updated", id);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var destination = await _destinations.GetForUpdateAsync(id, ct);
        if (destination is null)
        {
            return Result.NotFound(NotFoundMessage(id));
        }

        if (await _destinations.HasToursAsync(id, ct))
        {
            return Result.Conflict(HasTours);
        }

        _destinations.Remove(destination);
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ForeignKeyConstraintException)
        {
            // A tour was added between the check above and the delete; the foreign key refused the delete.
            return Result.Conflict(HasTours);
        }

        _logger.LogInformation("Destination {DestinationId} deleted", id);
        return Result.Success();
    }

    private static string NotFoundMessage(int id) => $"Destination {id} was not found.";

    /// <summary>" הכותל " and "הכותל" are the same name: compared and stored without outer spaces.</summary>
    private static void Trim(Destination destination)
    {
        destination.Name = destination.Name.Trim();
        destination.City = destination.City.Trim();
        destination.Description = string.IsNullOrWhiteSpace(destination.Description) ? null : destination.Description.Trim();
    }
}
