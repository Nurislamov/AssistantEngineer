using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;
using Microsoft.EntityFrameworkCore;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

internal sealed class BuildingHeatingReadModelRepository : IBuildingHeatingReadModelRepository
{
    private readonly AppDbContext _context;

    public BuildingHeatingReadModelRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<BuildingHeatingReadModel?> GetByIdAsync(
        int buildingId,
        CancellationToken cancellationToken = default) =>
        _context.Buildings
            .AsNoTracking()
            .Where(building => building.Id == buildingId)
            .Select(building => new BuildingHeatingReadModel(
                building.Id,
                building.Name,
                building.ProjectId,
                building.Project.Name,
                building.ClimateZone == null ? null : (double?)building.ClimateZone.WinterDesignTemperature.Celsius,
                building.Floors
                    .OrderBy(floor => floor.Id)
                    .SelectMany(floor => floor.Rooms
                        .OrderBy(room => room.Id)
                        .Select(room => new RoomHeatingReadModel(
                            room.Id,
                            room.Name,
                            room.Area.SquareMeters,
                            room.HeightM,
                            room.IndoorTemperature.Celsius,
                            room.OutdoorTemperatureOverride == null ? null : (double?)room.OutdoorTemperatureOverride.Celsius,
                            room.VentilationParameters == null
                                ? null
                                : new HeatingVentilationReadModel(
                                    room.VentilationParameters.AirChangesPerHour,
                                    room.VentilationParameters.HeatRecoveryEfficiency,
                                    room.VentilationParameters.InfiltrationAirChangesPerHour,
                                    room.VentilationParameters.StackCoefficient),
                            room.Windows
                                .OrderBy(window => window.Id)
                                .Select(window => new WindowHeatingReadModel(
                                    window.Area.SquareMeters,
                                    window.UValue.Value))
                                .ToList(),
                            room.Walls
                                .OrderBy(wall => wall.Id)
                                .Select(wall => new WallHeatingReadModel(
                                    wall.Area.SquareMeters,
                                    wall.IsExternal,
                                    wall.UValue.Value,
                                    wall.ConstructionAssembly == null
                                        ? new List<ConstructionLayerHeatingReadModel>()
                                        : wall.ConstructionAssembly.Layers
                                            .OrderBy(layer => layer.Id)
                                            .Select(layer => new ConstructionLayerHeatingReadModel(
                                                layer.ThicknessM,
                                                layer.Material.ThermalConductivityWPerMK))
                                            .ToList()))
                                .ToList())))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
