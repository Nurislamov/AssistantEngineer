using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Floors;

public class FloorQueryService
{
    private readonly IFloorRepository _floors;
    private readonly ILogger<FloorQueryService> _logger;

    public FloorQueryService(
        IFloorRepository floors,
        ILogger<FloorQueryService>? logger = null)
    {
        _floors = floors;
        _logger = logger ?? NullLogger<FloorQueryService>.Instance;
    }

    public async Task<Result<List<FloorResponse>>> GetByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        var floors = await _floors.ListByBuildingIdAsync(buildingId, cancellationToken);
        _logger.LogDebug("Loaded {FloorCount} floors for building {BuildingId}.", floors.Count, buildingId);
        return Result<List<FloorResponse>>.Success(floors.Select(BuildingsMapper.ToResponse).ToList());
    }

    public async Task<Result<FloorResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var floor = await _floors.GetByIdAsync(id, cancellationToken);
        if (floor is null)
        {
            _logger.LogWarning("Floor {FloorId} was not found.", id);
            return Result<FloorResponse>.NotFound($"Floor with id {id} not found.");
        }

        _logger.LogDebug("Loaded floor {FloorId}.", id);
        return Result<FloorResponse>.Success(BuildingsMapper.ToResponse(floor));
    }
}