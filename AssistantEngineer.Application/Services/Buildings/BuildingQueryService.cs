using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Responses;
using AssistantEngineer.Domain.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Application.Services.Buildings;

public class BuildingQueryService
{
    private readonly IBuildingRepository _buildings;
    private readonly ILogger<BuildingQueryService> _logger;

    public BuildingQueryService(
        IBuildingRepository buildings,
        ILogger<BuildingQueryService>? logger = null)
    {
        _buildings = buildings;
        _logger = logger ?? NullLogger<BuildingQueryService>.Instance;
    }

    public async Task<Result<List<BuildingResponse>>> GetByProjectIdAsync(
        int projectId,
        CancellationToken cancellationToken = default)
    {
        var buildings = await _buildings.ListByProjectIdAsync(projectId, cancellationToken);
        _logger.LogDebug("Loaded {BuildingCount} buildings for project {ProjectId}.", buildings.Count, projectId);
        return Result<List<BuildingResponse>>.Success(buildings.Select(ApplicationMapper.ToResponse).ToList());
    }

    public async Task<Result<BuildingResponse>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetByIdAsync(
            id,
            includeClimateZone: true,
            cancellationToken: cancellationToken);
        if (building is null)
        {
            _logger.LogWarning("Building {BuildingId} was not found.", id);
            return Result<BuildingResponse>.NotFound($"Building with id {id} not found.");
        }

        _logger.LogDebug("Loaded building {BuildingId}.", id);
        return Result<BuildingResponse>.Success(ApplicationMapper.ToResponse(building));
    }
}
