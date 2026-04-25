using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public sealed class BuildingModelValidationService
{
    private readonly IBuildingRepository _buildings;
    private readonly IAnnualClimateDataRepository _annualClimateData;
    private readonly BuildingAutocorrectionPlanner _autocorrectionPlanner;
    private readonly IRoomStandardDefaultsProvider _roomStandardDefaults;
    private readonly IRoomVentilationDefaultsProvider _roomVentilationDefaults;

    public BuildingModelValidationService(
        IBuildingRepository buildings,
        IAnnualClimateDataRepository annualClimateData,
        BuildingAutocorrectionPlanner autocorrectionPlanner,
        IRoomStandardDefaultsProvider roomStandardDefaults,
        IRoomVentilationDefaultsProvider roomVentilationDefaults)
    {
        _buildings = buildings;
        _annualClimateData = annualClimateData;
        _autocorrectionPlanner = autocorrectionPlanner;
        _roomStandardDefaults = roomStandardDefaults;
        _roomVentilationDefaults = roomVentilationDefaults;
    }

    public async Task<Result<BuildingValidationReport>> ValidateAsync(
        int buildingId,
        int weatherYear = 2020,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForValidationAsync(
            buildingId,
            asTracking: false,
            cancellationToken);

        if (building is null)
            return Result<BuildingValidationReport>.NotFound($"Building with id {buildingId} not found.");

        var autofixKeys = _autocorrectionPlanner
            .CreatePlan(building, new AutocorrectBuildingModelRequest())
            .Select(item => MakeKey(item.Code, item.Location))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var issues = new List<BuildingValidationIssue>();
        var rooms = building.Floors.SelectMany(floor => floor.Rooms).ToArray();
        var roomsById = rooms.ToDictionary(room => room.Id);

        void AddIssue(
            string code,
            BuildingCalculationReadinessSeverity severity,
            string location,
            string message)
        {
            issues.Add(new BuildingValidationIssue(
                code,
                severity,
                location,
                message,
                autofixKeys.Contains(MakeKey(code, location))));
        }

        if (building.ClimateZone is null)
        {
            AddIssue(
                BuildingValidationCodes.MissingClimateZone,
                BuildingCalculationReadinessSeverity.Error,
                "Building.ClimateZone",
                "Building climate zone is required.");
        }

        if (rooms.Length == 0)
        {
            AddIssue(
                BuildingValidationCodes.MissingRooms,
                BuildingCalculationReadinessSeverity.Error,
                "Building.Rooms",
                "At least one room is required.");
        }

        foreach (var room in rooms)
        {
            if (room.Area.SquareMeters <= 0)
            {
                AddIssue(
                    BuildingValidationCodes.RoomAreaNonPositive,
                    BuildingCalculationReadinessSeverity.Error,
                    $"Room[{room.Id}].Area",
                    "Room area must be positive.");
            }

            if (room.HeightM <= 0)
            {
                AddIssue(
                    BuildingValidationCodes.RoomHeightNonPositive,
                    BuildingCalculationReadinessSeverity.Error,
                    $"Room[{room.Id}].Height",
                    "Room height must be positive.");
            }

            if (room.PeopleCount < 0)
            {
                AddIssue(
                    BuildingValidationCodes.RoomPeopleCountNegative,
                    BuildingCalculationReadinessSeverity.Warning,
                    $"Room[{room.Id}].PeopleCount",
                    "People count should not be negative.");
            }

            if (room.EquipmentLoad.Watts < 0)
            {
                AddIssue(
                    BuildingValidationCodes.RoomEquipmentLoadNegative,
                    BuildingCalculationReadinessSeverity.Warning,
                    $"Room[{room.Id}].EquipmentLoad",
                    "Equipment load should not be negative.");
            }

            if (room.LightingLoad.Watts < 0)
            {
                AddIssue(
                    BuildingValidationCodes.RoomLightingLoadNegative,
                    BuildingCalculationReadinessSeverity.Warning,
                    $"Room[{room.Id}].LightingLoad",
                    "Lighting load should not be negative.");
            }

            if (room.Windows.Any() && !room.Walls.Any(wall => wall.IsExternal))
            {
                AddIssue(
                    BuildingValidationCodes.RoomWindowsWithoutExternalWall,
                    BuildingCalculationReadinessSeverity.Error,
                    $"Room[{room.Id}].Windows",
                    "Rooms with windows need at least one external wall.");
            }

            if (room.Area.SquareMeters > 0)
            {
                var totalWindowArea = room.Windows.Sum(window => window.Area.SquareMeters);
                if (totalWindowArea > room.Area.SquareMeters * 0.8)
                {
                    AddIssue(
                        BuildingValidationCodes.RoomWindowAreaTooLarge,
                        BuildingCalculationReadinessSeverity.Warning,
                        $"Room[{room.Id}].Windows",
                        "Total window area is greater than 80% of room floor area.");
                }
            }

            foreach (var wall in room.Walls)
            {
                if (wall.BoundaryType is WallBoundaryType.AdjacentConditioned or WallBoundaryType.AdjacentUnconditioned)
                {
                    if (!wall.AdjacentRoomId.HasValue)
                    {
                        AddIssue(
                            BuildingValidationCodes.WallAdjacentRoomRequired,
                            BuildingCalculationReadinessSeverity.Error,
                            $"Room[{room.Id}].Wall[{wall.Id}]",
                            "Adjacent wall boundary type requires AdjacentRoomId.");
                        continue;
                    }

                    if (!roomsById.TryGetValue(wall.AdjacentRoomId.Value, out var adjacentRoom))
                    {
                        AddIssue(
                            BuildingValidationCodes.WallAdjacentRoomInvalid,
                            BuildingCalculationReadinessSeverity.Error,
                            $"Room[{room.Id}].Wall[{wall.Id}]",
                            $"Adjacent room {wall.AdjacentRoomId.Value} must belong to the same building.");
                        continue;
                    }

                    if (adjacentRoom.Id == room.Id)
                    {
                        AddIssue(
                            BuildingValidationCodes.WallAdjacentRoomSelfReference,
                            BuildingCalculationReadinessSeverity.Error,
                            $"Room[{room.Id}].Wall[{wall.Id}]",
                            "A wall cannot reference the same room as its adjacent room.");
                    }
                }
                else if (wall.AdjacentRoomId.HasValue)
                {
                    AddIssue(
                        BuildingValidationCodes.WallUnexpectedAdjacentRoomReference,
                        BuildingCalculationReadinessSeverity.Warning,
                        $"Room[{room.Id}].Wall[{wall.Id}]",
                        "AdjacentRoomId is set for a non-adjacent wall boundary type.");
                }
            }

            AddRoomDefaultAvailabilityIssues(room, AddIssue);
            AddVentilationDefaultAvailabilityIssue(room, AddIssue);
        }

        var duplicatedZoneRooms = building.ThermalZones
            .SelectMany(zone => zone.AssignedRooms)
            .GroupBy(room => room.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(id => id)
            .ToArray();

        foreach (var roomId in duplicatedZoneRooms)
        {
            AddIssue(
                BuildingValidationCodes.ThermalZoneRoomAssignedMultipleTimes,
                BuildingCalculationReadinessSeverity.Error,
                "ThermalZones",
                $"Room {roomId} is assigned to multiple thermal zones.");
        }

        if (building.ClimateZone is not null)
        {
            var annualData = await _annualClimateData.GetForClimateZoneAsync(
                building.ClimateZone.Id,
                weatherYear,
                cancellationToken);

            var annualHourCount = annualData?.HourlyData.Count ?? 0;
            if (annualHourCount != 8760)
            {
                AddIssue(
                    BuildingValidationCodes.AnnualClimateDataHoursInvalid,
                    BuildingCalculationReadinessSeverity.Warning,
                    "AnnualClimateData",
                    $"ISO 52016 annual calculation expects 8760 weather hours; found {annualHourCount}.");
            }
        }

        var orderedIssues = issues
            .OrderByDescending(issue => issue.Severity)
            .ThenBy(issue => issue.Location, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var report = new BuildingValidationReport
        {
            BuildingId = building.Id,
            BuildingName = building.Name,
            IsValid = orderedIssues.All(issue => issue.Severity != BuildingCalculationReadinessSeverity.Error),
            ErrorsCount = orderedIssues.Count(issue => issue.Severity == BuildingCalculationReadinessSeverity.Error),
            WarningsCount = orderedIssues.Count(issue => issue.Severity == BuildingCalculationReadinessSeverity.Warning),
            AutoFixableIssuesCount = orderedIssues.Count(issue => issue.CanAutoFix),
            Issues = orderedIssues
        };

        return Result<BuildingValidationReport>.Success(report);
    }

    private void AddRoomDefaultAvailabilityIssues(
        Room room,
        Action<string, BuildingCalculationReadinessSeverity, string, string> addIssue)
    {
        if (room.Area.SquareMeters <= 0)
            return;

        var defaults = _roomStandardDefaults.GetDefaults(room);

        if (room.PeopleCount <= 0 && defaults.SuggestedPeopleCount > 0)
        {
            addIssue(
                BuildingValidationCodes.RoomPeopleCountDefaultAvailable,
                BuildingCalculationReadinessSeverity.Warning,
                $"Room[{room.Id}].PeopleCount",
                RoomDefaultSuggestionFormatter.BuildPeopleMessage(defaults));
        }

        if (room.EquipmentLoad.Watts <= 0 && defaults.EquipmentLoadWatts > 0)
        {
            addIssue(
                BuildingValidationCodes.RoomEquipmentLoadDefaultAvailable,
                BuildingCalculationReadinessSeverity.Warning,
                $"Room[{room.Id}].EquipmentLoad",
                RoomDefaultSuggestionFormatter.BuildEquipmentMessage(defaults));
        }

        if (room.LightingLoad.Watts <= 0 && defaults.LightingLoadWatts > 0)
        {
            addIssue(
                BuildingValidationCodes.RoomLightingLoadDefaultAvailable,
                BuildingCalculationReadinessSeverity.Warning,
                $"Room[{room.Id}].LightingLoad",
                RoomDefaultSuggestionFormatter.BuildLightingMessage(defaults));
        }
    }

    private void AddVentilationDefaultAvailabilityIssue(
        Room room,
        Action<string, BuildingCalculationReadinessSeverity, string, string> addIssue)
    {
        if (room.VentilationParameters is not null)
            return;

        var defaults = _roomVentilationDefaults.GetDefaults(room);
        if (!defaults.CanApply)
            return;

        addIssue(
            BuildingValidationCodes.RoomVentilationDefaultsAvailable,
            BuildingCalculationReadinessSeverity.Warning,
            $"Room[{room.Id}].VentilationParameters",
            $"Suggested ventilation defaults are available from TB14 reference data. Proposed ACH = {defaults.ProposedAirChangesPerHour:F3}, outdoor air = {defaults.DesignOutdoorAirLitersPerSecond:F3} L/s.");
    }

    private static string MakeKey(string code, string location) => $"{code}::{location}";
}