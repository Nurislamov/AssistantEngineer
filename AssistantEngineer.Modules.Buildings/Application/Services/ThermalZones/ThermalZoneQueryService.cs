using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Mappers;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Services.ThermalZones;

public sealed class ThermalZoneQueryService
{
    private readonly IBuildingRepository _buildings;

    public ThermalZoneQueryService(IBuildingRepository buildings)
    {
        _buildings = buildings;
    }

    public async Task<Result<List<ThermalZoneResponse>>> GetByBuildingIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetWithThermalZonesAndRoomsAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<List<ThermalZoneResponse>>.NotFound($"Building with id {buildingId} not found.");

        var response = building.ThermalZones
            .OrderBy(zone => zone.Id)
            .Select(BuildingsMapper.ToResponse)
            .ToList();

        return Result<List<ThermalZoneResponse>>.Success(response);
    }

    public async Task<Result<ThermalZoneResponse>> GetByIdAsync(
        int thermalZoneId,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetByThermalZoneIdAsync(thermalZoneId, cancellationToken);
        if (building is null)
            return Result<ThermalZoneResponse>.NotFound($"Thermal zone with id {thermalZoneId} not found.");

        var zone = building.ThermalZones.FirstOrDefault(x => x.Id == thermalZoneId);
        if (zone is null)
            return Result<ThermalZoneResponse>.NotFound($"Thermal zone with id {thermalZoneId} not found.");

        return Result<ThermalZoneResponse>.Success(BuildingsMapper.ToResponse(zone));
    }
}