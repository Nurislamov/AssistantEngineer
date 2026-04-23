using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public sealed class BuildingCalculationReadinessService
{
    private readonly IBuildingRepository _buildings;
    private readonly IAnnualClimateDataRepository _annualClimateData;

    public BuildingCalculationReadinessService(
        IBuildingRepository buildings,
        IAnnualClimateDataRepository annualClimateData)
    {
        _buildings = buildings;
        _annualClimateData = annualClimateData;
    }

    public async Task<Result<BuildingCalculationReadinessReport>> CheckAsync(
        int buildingId,
        int weatherYear = 2020,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<BuildingCalculationReadinessReport>.NotFound($"Building with id {buildingId} not found.");

        var issues = new List<BuildingCalculationReadinessIssue>();
        var rooms = building.Floors.SelectMany(floor => floor.Rooms).ToArray();
        var roomsById = rooms.ToDictionary(room => room.Id);

        if (building.ClimateZone is null)
            issues.Add(Error("Building.ClimateZone", "Building climate zone is required."));

        if (rooms.Length == 0)
            issues.Add(Error("Building.Rooms", "At least one room is required."));

        foreach (var room in rooms)
        {
            if (room.Area.SquareMeters <= 0)
                issues.Add(Error($"Room[{room.Id}].Area", "Room area must be positive."));

            if (room.HeightM <= 0)
                issues.Add(Error($"Room[{room.Id}].Height", "Room height must be positive."));

            if (room.Windows.Any() && !room.Walls.Any(wall => wall.IsExternal))
                issues.Add(Error($"Room[{room.Id}].Windows", "Rooms with windows need at least one external wall."));

            var totalWindowArea = room.Windows.Sum(window => window.Area.SquareMeters);
            if (totalWindowArea > room.Area.SquareMeters * 0.8)
                issues.Add(Warning($"Room[{room.Id}].Windows", "Total window area is greater than 80% of room floor area."));

            foreach (var wall in room.Walls)
            {
                if (wall.BoundaryType is WallBoundaryType.AdjacentConditioned or WallBoundaryType.AdjacentUnconditioned)
                {
                    if (!wall.AdjacentRoomId.HasValue)
                    {
                        issues.Add(Error(
                            $"Room[{room.Id}].Wall[{wall.Id}]",
                            "Adjacent wall boundary type requires AdjacentRoomId."));
                        continue;
                    }

                    if (!roomsById.TryGetValue(wall.AdjacentRoomId.Value, out var adjacentRoom))
                    {
                        issues.Add(Error(
                            $"Room[{room.Id}].Wall[{wall.Id}]",
                            $"Adjacent room {wall.AdjacentRoomId.Value} must belong to the same building."));
                        continue;
                    }

                    if (adjacentRoom.Id == room.Id)
                    {
                        issues.Add(Error(
                            $"Room[{room.Id}].Wall[{wall.Id}]",
                            "A wall cannot reference the same room as its adjacent room."));
                    }
                }
                else if (wall.AdjacentRoomId.HasValue)
                {
                    issues.Add(Warning(
                        $"Room[{room.Id}].Wall[{wall.Id}]",
                        "AdjacentRoomId is set for a non-adjacent wall boundary type."));
                }
            }
        }

        var duplicatedZoneRooms = building.ThermalZones
            .SelectMany(zone => zone.AssignedRooms)
            .GroupBy(room => room)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        foreach (var room in duplicatedZoneRooms)
            issues.Add(Error("ThermalZones", $"Room {room.Id} is assigned to multiple thermal zones."));

        if (building.ClimateZone is not null)
        {
            var annualData = await _annualClimateData.GetForClimateZoneAsync(
                building.ClimateZone.Id,
                weatherYear,
                cancellationToken);

            var annualHourCount = annualData?.HourlyData.Count ?? 0;
            if (annualHourCount != 8760)
                issues.Add(Warning("AnnualClimateData", $"ISO 52016 annual calculation expects 8760 weather hours; found {annualHourCount}."));
        }

        var report = new BuildingCalculationReadinessReport
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            IsReady = issues.All(issue => issue.Severity != BuildingCalculationReadinessSeverity.Error),
            Issues = issues
        };

        return Result<BuildingCalculationReadinessReport>.Success(report);
    }

    private static BuildingCalculationReadinessIssue Error(string path, string message) =>
        new(BuildingCalculationReadinessSeverity.Error, path, message);

    private static BuildingCalculationReadinessIssue Warning(string path, string message) =>
        new(BuildingCalculationReadinessSeverity.Warning, path, message);
}