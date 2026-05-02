using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Abstractions;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Floors;

public class FloorCommandService
{
    private readonly IBuildingRepository _buildings;
    private readonly IFloorRepository _floors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FloorCommandService> _logger;

    public FloorCommandService(
        IBuildingRepository buildings,
        IFloorRepository floors,
        IUnitOfWork unitOfWork,
        ILogger<FloorCommandService>? logger = null)
    {
        _buildings = buildings;
        _floors = floors;
        _unitOfWork = unitOfWork;
        _logger = logger ?? NullLogger<FloorCommandService>.Instance;
    }

    public async Task<Result<FloorResponse>> CreateAsync(
        int buildingId,
        CreateFloorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating floor with name {FloorName} for building {BuildingId}.", request.Name, buildingId);

        var building = await _buildings.GetWithFloorsAsync(buildingId, cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Cannot create floor because building {BuildingId} was not found.", buildingId);
            return Result<FloorResponse>.NotFound($"Building with id {buildingId} not found.");
        }

        var floorResult = building.AddFloor(request.Name);
        if (floorResult.IsFailure)
        {
            _logger.LogWarning(
                "Floor creation failed for building {BuildingId} with name {FloorName}: {Error}.",
                buildingId,
                request.Name,
                floorResult.Error);
            return Result<FloorResponse>.Failure(floorResult);
        }

        _floors.Add(floorResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created floor {FloorId} for building {BuildingId}.", floorResult.Value.Id, buildingId);
        return Result<FloorResponse>.Success(BuildingsMapper.ToResponse(floorResult.Value));
    }

    public async Task<Result<FloorResponse>> UpdateAsync(
        int floorId,
        UpdateFloorRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating floor {FloorId}.", floorId);

        var floor = await _floors.GetByIdAsync(floorId, cancellationToken);
        if (floor is null)
        {
            _logger.LogWarning("Cannot update floor because floor {FloorId} was not found.", floorId);
            return Result<FloorResponse>.NotFound($"Floor with id {floorId} not found.");
        }

        var building = await _buildings.GetWithFloorsAsync(floor.BuildingId, cancellationToken);
        if (building is null)
            return Result<FloorResponse>.Validation("Unable to validate floor building ownership.");

        if (building.Floors.Any(existing =>
                existing.Id != floorId &&
                existing.Name.Equals((request.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Result<FloorResponse>.Conflict($"Floor with name '{request.Name}' already exists in this building.");
        }

        var updateResult = floor.UpdateName(request.Name);
        if (updateResult.IsFailure)
            return Result<FloorResponse>.Failure(updateResult);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated floor {FloorId}.", floorId);
        return Result<FloorResponse>.Success(BuildingsMapper.ToResponse(floor));
    }

    public async Task<Result> DeleteAsync(
        int floorId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting floor {FloorId}.", floorId);

        var floor = await _floors.GetByIdAsync(floorId, cancellationToken);
        if (floor is null)
        {
            _logger.LogWarning("Cannot delete floor because floor {FloorId} was not found.", floorId);
            return Result.NotFound($"Floor with id {floorId} not found.");
        }

        _floors.Remove(floor);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted floor {FloorId}.", floorId);
        return Result.Success();
    }
}
