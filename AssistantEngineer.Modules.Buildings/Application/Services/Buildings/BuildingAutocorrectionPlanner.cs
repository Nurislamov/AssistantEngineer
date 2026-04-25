using AssistantEngineer.Modules.Buildings.Application.Abstractions.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Models.StandardDefaults;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.ThermalZones;
using AssistantEngineer.Modules.Buildings.Domain.Ventilation;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Modules.Buildings.Application.Services.Buildings;

public sealed class BuildingAutocorrectionPlanner
{
    private readonly IRoomStandardDefaultsProvider _roomStandardDefaults;
    private readonly IRoomVentilationDefaultsProvider _roomVentilationDefaults;

    public BuildingAutocorrectionPlanner(
        IRoomStandardDefaultsProvider roomStandardDefaults,
        IRoomVentilationDefaultsProvider roomVentilationDefaults)
    {
        _roomStandardDefaults = roomStandardDefaults;
        _roomVentilationDefaults = roomVentilationDefaults;
    }

    public IReadOnlyList<BuildingAutocorrectionPlanItem> CreatePlan(
        Building building,
        AutocorrectBuildingModelRequest options)
    {
        var plan = new List<BuildingAutocorrectionPlanItem>();
        var rooms = building.Floors
            .SelectMany(floor => floor.Rooms)
            .OrderBy(room => room.Id)
            .ToArray();

        foreach (var room in rooms)
        {
            if (room.Area.SquareMeters <= 0)
            {
                var normalizedArea = Math.Max(options.MinimumRoomAreaM2, 0.1);
                var areaResult = Area.FromSquareMeters(normalizedArea);

                if (areaResult.IsSuccess)
                {
                    var oldValue = room.Area.SquareMeters.ToString("F3");
                    var newValue = areaResult.Value.SquareMeters.ToString("F3");

                    plan.Add(new BuildingAutocorrectionPlanItem(
                        BuildingValidationCodes.RoomAreaNonPositive,
                        $"Room[{room.Id}].Area",
                        $"Normalize room area to {newValue} m2.",
                        oldValue,
                        newValue,
                        () => room.UpdateArea(areaResult.Value)));
                }
            }

            if (room.HeightM <= 0)
            {
                var normalizedHeight = Math.Max(options.DefaultRoomHeightM, 0.1);
                var oldValue = room.HeightM.ToString("F3");
                var newValue = normalizedHeight.ToString("F3");

                plan.Add(new BuildingAutocorrectionPlanItem(
                    BuildingValidationCodes.RoomHeightNonPositive,
                    $"Room[{room.Id}].Height",
                    $"Normalize room height to {newValue} m.",
                    oldValue,
                    newValue,
                    () => room.UpdateHeight(normalizedHeight)));
            }

            if (options.ClampNegativePeopleCountToZero && room.PeopleCount < 0)
            {
                plan.Add(new BuildingAutocorrectionPlanItem(
                    BuildingValidationCodes.RoomPeopleCountNegative,
                    $"Room[{room.Id}].PeopleCount",
                    "Clamp negative people count to 0.",
                    room.PeopleCount.ToString(),
                    "0",
                    () => room.UpdatePeopleCount(0)));
            }

            if (options.ClampNegativeLoadsToZero && room.EquipmentLoad.Watts < 0)
            {
                var zeroLoad = Power.FromWatts(0).Value;

                plan.Add(new BuildingAutocorrectionPlanItem(
                    BuildingValidationCodes.RoomEquipmentLoadNegative,
                    $"Room[{room.Id}].EquipmentLoad",
                    "Clamp negative equipment load to 0 W.",
                    room.EquipmentLoad.Watts.ToString("F3"),
                    "0",
                    () => room.UpdateEquipmentLoad(zeroLoad)));
            }

            if (options.ClampNegativeLoadsToZero && room.LightingLoad.Watts < 0)
            {
                var zeroLoad = Power.FromWatts(0).Value;

                plan.Add(new BuildingAutocorrectionPlanItem(
                    BuildingValidationCodes.RoomLightingLoadNegative,
                    $"Room[{room.Id}].LightingLoad",
                    "Clamp negative lighting load to 0 W.",
                    room.LightingLoad.Watts.ToString("F3"),
                    "0",
                    () => room.UpdateLightingLoad(zeroLoad)));
            }

            if (options.ResizeOversizedWindows && room.Windows.Count > 0)
            {
                var effectiveArea = room.Area.SquareMeters > 0
                    ? room.Area.SquareMeters
                    : Math.Max(options.MinimumRoomAreaM2, 0.1);

                var totalWindowArea = room.Windows.Sum(window => window.Area.SquareMeters);
                var maxWindowArea = effectiveArea * options.MaximumWindowToFloorAreaRatio;

                if (maxWindowArea > 0 && totalWindowArea > maxWindowArea)
                {
                    var ratio = maxWindowArea / totalWindowArea;
                    var oldValue = totalWindowArea.ToString("F3");
                    var newValue = maxWindowArea.ToString("F3");

                    plan.Add(new BuildingAutocorrectionPlanItem(
                        BuildingValidationCodes.RoomWindowAreaTooLarge,
                        $"Room[{room.Id}].Windows",
                        $"Scale all window areas proportionally to keep total glazing at {options.MaximumWindowToFloorAreaRatio:P0} of effective room area.",
                        oldValue,
                        newValue,
                        () => ResizeRoomWindows(room, ratio)));
                }
            }

            AddRoomTypeDefaultsPlanItems(room, options, plan);
            AddVentilationDefaultsPlanItems(room, options, plan);

            foreach (var wall in room.Walls.OrderBy(wall => wall.Id))
            {
                if (options.RemoveUnexpectedAdjacentRoomReferences &&
                    wall.BoundaryType is not WallBoundaryType.AdjacentConditioned and not WallBoundaryType.AdjacentUnconditioned &&
                    wall.AdjacentRoomId.HasValue)
                {
                    plan.Add(new BuildingAutocorrectionPlanItem(
                        BuildingValidationCodes.WallUnexpectedAdjacentRoomReference,
                        $"Room[{room.Id}].Wall[{wall.Id}]",
                        "Remove unexpected AdjacentRoom reference from a non-adjacent wall.",
                        wall.AdjacentRoomId.Value.ToString(),
                        null,
                        wall.ClearUnexpectedAdjacentRoomReference));
                }
            }
        }

        if (options.RemoveDuplicateThermalZoneAssignments)
            BuildThermalZoneDuplicatePlans(building, plan);

        return plan;
    }

    private void AddRoomTypeDefaultsPlanItems(
        Room room,
        AutocorrectBuildingModelRequest options,
        ICollection<BuildingAutocorrectionPlanItem> plan)
    {
        if (!options.ApplyRoomTypeInternalLoadDefaults)
            return;

        var effectiveArea = room.Area.SquareMeters > 0
            ? room.Area.SquareMeters
            : Math.Max(options.MinimumRoomAreaM2, 0.1);

        if (effectiveArea <= 0)
            return;

        var defaults = _roomStandardDefaults.GetDefaults(room);

        if (options.ApplyPeopleCountDefaultsWhenMissing &&
            room.PeopleCount <= 0 &&
            defaults.SuggestedPeopleCount > 0)
        {
            plan.Add(new BuildingAutocorrectionPlanItem(
                BuildingValidationCodes.RoomPeopleCountDefaultAvailable,
                $"Room[{room.Id}].PeopleCount",
                RoomDefaultSuggestionFormatter.BuildPeopleMessage(defaults),
                room.PeopleCount.ToString(),
                defaults.SuggestedPeopleCount.ToString(),
                () => room.UpdatePeopleCount(defaults.SuggestedPeopleCount)));
        }

        if (options.ApplyEquipmentLoadDefaultsWhenMissing &&
            room.EquipmentLoad.Watts <= 0 &&
            defaults.EquipmentLoadWatts > 0)
        {
            var targetLoad = Power.FromWatts(defaults.EquipmentLoadWatts);

            if (targetLoad.IsSuccess)
            {
                plan.Add(new BuildingAutocorrectionPlanItem(
                    BuildingValidationCodes.RoomEquipmentLoadDefaultAvailable,
                    $"Room[{room.Id}].EquipmentLoad",
                    RoomDefaultSuggestionFormatter.BuildEquipmentMessage(defaults),
                    room.EquipmentLoad.Watts.ToString("F3"),
                    defaults.EquipmentLoadWatts.ToString("F3"),
                    () => room.UpdateEquipmentLoad(targetLoad.Value)));
            }
        }

        if (options.ApplyLightingLoadDefaultsWhenMissing &&
            room.LightingLoad.Watts <= 0 &&
            defaults.LightingLoadWatts > 0)
        {
            var targetLoad = Power.FromWatts(defaults.LightingLoadWatts);

            if (targetLoad.IsSuccess)
            {
                plan.Add(new BuildingAutocorrectionPlanItem(
                    BuildingValidationCodes.RoomLightingLoadDefaultAvailable,
                    $"Room[{room.Id}].LightingLoad",
                    RoomDefaultSuggestionFormatter.BuildLightingMessage(defaults),
                    room.LightingLoad.Watts.ToString("F3"),
                    defaults.LightingLoadWatts.ToString("F3"),
                    () => room.UpdateLightingLoad(targetLoad.Value)));
            }
        }
    }

    private void AddVentilationDefaultsPlanItems(
        Room room,
        AutocorrectBuildingModelRequest options,
        ICollection<BuildingAutocorrectionPlanItem> plan)
    {
        if (!options.ApplyVentilationDefaultsWhenMissing)
            return;

        if (room.VentilationParameters is not null)
            return;

        var defaults = _roomVentilationDefaults.GetDefaults(room);
        if (!defaults.CanApply)
            return;

        plan.Add(new BuildingAutocorrectionPlanItem(
            BuildingValidationCodes.RoomVentilationDefaultsAvailable,
            $"Room[{room.Id}].VentilationParameters",
            $"Apply TB14-derived ventilation defaults. Proposed ACH = {defaults.ProposedAirChangesPerHour:F3}.",
            null,
            defaults.ProposedAirChangesPerHour.ToString("F3"),
            () => ApplyVentilationDefaults(room, defaults)));
    }

    private static Result ApplyVentilationDefaults(Room room, RoomVentilationDefaults defaults)
    {
        var createResult = VentilationParameters.Create(
            defaults.ProposedAirChangesPerHour,
            defaults.HeatRecoveryEfficiency,
            defaults.InfiltrationAirChangesPerHour,
            defaults.WindExposureFactor,
            defaults.StackCoefficient,
            defaults.WindCoefficient);

        if (createResult.IsFailure)
            return createResult;

        return room.SetVentilationParameters(createResult.Value);
    }

    private static Result ResizeRoomWindows(Room room, double ratio)
    {
        foreach (var window in room.Windows)
        {
            var resizedAreaValue = Math.Max(window.Area.SquareMeters * ratio, 0.01);
            var resizedArea = Area.FromSquareMeters(resizedAreaValue);

            if (resizedArea.IsFailure)
                return Result.Failure(resizedArea.Error);

            var resizeResult = window.Resize(resizedArea.Value);
            if (resizeResult.IsFailure)
                return resizeResult;
        }

        return Result.Success();
    }

    private static void BuildThermalZoneDuplicatePlans(
        Building building,
        ICollection<BuildingAutocorrectionPlanItem> plan)
    {
        var firstZoneByRoomId = new Dictionary<int, ThermalZone>();

        foreach (var zone in building.ThermalZones.OrderBy(zone => zone.Id))
        {
            var uniqueRooms = new List<Room>();
            var duplicateRooms = new List<Room>();

            foreach (var room in zone.AssignedRooms.OrderBy(room => room.Id))
            {
                if (firstZoneByRoomId.TryAdd(room.Id, zone))
                    uniqueRooms.Add(room);
                else
                    duplicateRooms.Add(room);
            }

            if (duplicateRooms.Count == 0)
                continue;

            if (uniqueRooms.Count == 0)
                continue;

            var duplicateIds = string.Join(", ", duplicateRooms.Select(room => room.Id));
            var remainingIds = string.Join(", ", uniqueRooms.Select(room => room.Id));

            plan.Add(new BuildingAutocorrectionPlanItem(
                BuildingValidationCodes.ThermalZoneRoomAssignedMultipleTimes,
                $"ThermalZone[{zone.Id}]",
                $"Remove duplicate room assignments from thermal zone {zone.Name}.",
                duplicateIds,
                remainingIds,
                () =>
                {
                    zone.ReplaceRooms(uniqueRooms);
                    return Result.Success();
                }));
        }
    }
}

public sealed class BuildingAutocorrectionPlanItem
{
    private readonly Func<Result> _apply;

    public BuildingAutocorrectionPlanItem(
        string code,
        string location,
        string description,
        string? oldValue,
        string? newValue,
        Func<Result> apply)
    {
        Code = code;
        Location = location;
        Description = description;
        OldValue = oldValue;
        NewValue = newValue;
        _apply = apply;
    }

    public string Code { get; }
    public string Location { get; }
    public string Description { get; }
    public string? OldValue { get; }
    public string? NewValue { get; }

    public Result Apply() => _apply();

    public BuildingAutocorrectionAction ToContract(bool applied) =>
        new()
        {
            Code = Code,
            Location = Location,
            Description = Description,
            OldValue = OldValue,
            NewValue = NewValue,
            Applied = applied
        };
}