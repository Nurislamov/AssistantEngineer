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
}
