using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Buildings.Application.Mappers;

internal static class BuildingsMapper
{
    public static ProjectResponse ToResponse(Project project) =>
        new() { Id = project.Id, Name = project.Name };

    public static BuildingResponse ToResponse(Building building) =>
        new()
        {
            Id = building.Id,
            Name = building.Name,
            ProjectId = building.ProjectId,
            ClimateZoneId = building.ClimateZone?.Id,
            ClimateZoneName = building.ClimateZone?.Name
        };

    public static FloorResponse ToResponse(Floor floor) =>
        new() { Id = floor.Id, Name = floor.Name, BuildingId = floor.BuildingId };

    public static RoomResponse ToResponse(Room room) =>
        new()
        {
            Id = room.Id,
            Name = room.Name,
            AreaM2 = room.Area.SquareMeters,
            HeightM = room.HeightM,
            VolumeM3 = room.CalculateVolume(),
            IndoorTemperatureC = room.IndoorTemperature.Celsius,
            OutdoorTemperatureOverrideC = room.OutdoorTemperatureOverride?.Celsius,
            PeopleCount = room.PeopleCount,
            EquipmentLoadW = room.EquipmentLoad.Watts,
            LightingLoadW = room.LightingLoad.Watts,
            Type = room.Type.ToContract(),
            FloorId = room.FloorId
        };

    public static WindowResponse ToResponse(Window window) =>
        new()
        {
            Id = window.Id,
            AreaM2 = window.Area.SquareMeters,
            UValue = window.UValue.Value,
            Shgc = window.Shgc.Value,
            Orientation = window.Orientation.ToContract(),
            RoomId = window.RoomId,
            Shading = new WindowShadingParametersResponse
            {
                OverhangDepthM = window.Shading.OverhangDepthM,
                SideFinDepthM = window.Shading.SideFinDepthM,
                RevealDepthM = window.Shading.RevealDepthM,
                WindowHeightM = window.Shading.WindowHeightM,
                WindowWidthM = window.Shading.WindowWidthM,
                MinimumDirectSolarReductionFactor = window.Shading.MinimumDirectSolarReductionFactor,
                DiffuseSolarShareUnaffected = window.Shading.DiffuseSolarShareUnaffected
            }
        };

    public static WallResponse ToResponse(Wall wall) =>
        new()
        {
            Id = wall.Id,
            AreaM2 = wall.Area.SquareMeters,
            IsExternal = wall.IsExternal,
            UValue = wall.UValue.Value,
            Orientation = wall.Orientation.ToContract(),
            RoomId = wall.RoomId
        };
}
