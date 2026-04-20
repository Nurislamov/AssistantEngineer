using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Requests;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Floors;

public class FloorCommandService
{
    private readonly IBuildingRepository _buildings;
    private readonly IFloorRepository _floors;
    private readonly IAppDbContext _context;
    private readonly ILogger<FloorCommandService> _logger;

    public FloorCommandService(
        IBuildingRepository buildings,
        IFloorRepository floors,
        IAppDbContext context,
        ILogger<FloorCommandService>? logger = null)
    {
        _buildings = buildings;
        _floors = floors;
        _context = context;
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
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created floor {FloorId} for building {BuildingId}.", floorResult.Value.Id, buildingId);
        return Result<FloorResponse>.Success(ApplicationMapper.ToResponse(floorResult.Value));
    }
}
