using AssistantEngineer.Modules.Buildings.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Schedules;

namespace AssistantEngineer.Modules.Buildings.Application.Mappers;

public static class RoomCalculationInputSnapshotMapper
{
    public static RoomCalculationInputSnapshot ToCalculationInputSnapshot(this Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var walls = room.Walls
            .OrderBy(wall => wall.Id)
            .Select(wall => new RoomCalculationWallSnapshot
            {
                WallId = wall.Id,
                AreaM2 = wall.Area.SquareMeters,
                UValueWPerM2K = wall.UValue.Value,
                Orientation = wall.Orientation.ToContract(),
                BoundaryType = wall.BoundaryType.ToContract(),
                IsExternal = wall.IsExternal,
                AdjacentRoomId = wall.AdjacentRoomId
            })
            .ToArray();

        var windows = room.Windows
            .OrderBy(window => window.Id)
            .Select(window => new RoomCalculationWindowSnapshot
            {
                WindowId = window.Id,
                AreaM2 = window.Area.SquareMeters,
                UValueWPerM2K = window.UValue.Value,
                Shgc = window.Shgc.Value,
                Orientation = window.Orientation.ToContract(),
                OverhangDepthM = window.Shading.OverhangDepthM,
                SideFinDepthM = window.Shading.SideFinDepthM,
                RevealDepthM = window.Shading.RevealDepthM,
                WindowHeightM = window.Shading.WindowHeightM,
                WindowWidthM = window.Shading.WindowWidthM,
                MinimumDirectSolarReductionFactor = window.Shading.MinimumDirectSolarReductionFactor,
                DiffuseSolarShareUnaffected = window.Shading.DiffuseSolarShareUnaffected
            })
            .ToArray();

        var envelope = walls
            .Select(wall => new RoomEnvelopeElementSnapshot
            {
                Kind = "Wall",
                ElementId = wall.WallId,
                AreaM2 = wall.AreaM2,
                UValueWPerM2K = wall.UValueWPerM2K,
                Orientation = wall.Orientation
            })
            .Concat(windows.Select(window => new RoomEnvelopeElementSnapshot
            {
                Kind = "Window",
                ElementId = window.WindowId,
                AreaM2 = window.AreaM2,
                UValueWPerM2K = window.UValueWPerM2K,
                Orientation = window.Orientation
            }))
            .ToArray();

        var climateZone = room.Floor.Building.ClimateZone;
        var ventilation = room.VentilationParameters;
        var ground = room.GroundContactMetadata;

        return new RoomCalculationInputSnapshot
        {
            RoomId = room.Id,
            FloorId = room.FloorId,
            BuildingId = room.Floor.BuildingId,
            ProjectId = room.Floor.Building.ProjectId,
            RoomName = room.Name,
            RoomType = room.Type.ToContract(),
            AreaM2 = room.Area.SquareMeters,
            HeightM = room.HeightM,
            VolumeM3 = room.CalculateVolume(),
            IndoorTemperatureC = room.IndoorTemperature.Celsius,
            OutdoorTemperatureOverrideC = room.OutdoorTemperatureOverride?.Celsius,
            PeopleCount = room.PeopleCount,
            EquipmentLoadW = room.EquipmentLoad.Watts,
            LightingLoadW = room.LightingLoad.Watts,
            OccupancyScheduleId = room.OccupancyScheduleId,
            EquipmentScheduleId = room.EquipmentScheduleId,
            LightingScheduleId = room.LightingScheduleId,
            Climate = climateZone is null
                ? null
                : new RoomCalculationClimateInputSnapshot
                {
                    ClimateZoneId = climateZone.Id,
                    ClimateZoneName = climateZone.Name,
                    SummerDesignTemperatureC = climateZone.SummerDesignTemperature.Celsius,
                    WinterDesignTemperatureC = climateZone.WinterDesignTemperature.Celsius
                },
            Ventilation = ventilation is null
                ? null
                : new RoomCalculationVentilationInputSnapshot
                {
                    VentilationParametersId = ventilation.Id,
                    AirChangesPerHour = ventilation.AirChangesPerHour,
                    HeatRecoveryEfficiency = ventilation.HeatRecoveryEfficiency,
                    InfiltrationAirChangesPerHour = ventilation.InfiltrationAirChangesPerHour,
                    WindExposureFactor = ventilation.WindExposureFactor,
                    StackCoefficient = ventilation.StackCoefficient,
                    WindCoefficient = ventilation.WindCoefficient
                },
            GroundBoundary = ground is null
                ? null
                : new RoomCalculationGroundBoundaryInputSnapshot
                {
                    ContactType = ground.ContactType.ToContract(),
                    ExposedPerimeterM = ground.ExposedPerimeterM,
                    BurialDepthM = ground.BurialDepthM,
                    WallHeightBelowGradeM = ground.WallHeightBelowGradeM,
                    HorizontalInsulationWidthM = ground.HorizontalInsulationWidthM,
                    PerimeterInsulationDepthM = ground.PerimeterInsulationDepthM,
                    UnderfloorVentilationAirChangesPerHour = ground.UnderfloorVentilationAirChangesPerHour
                },
            Walls = walls,
            Windows = windows,
            EnvelopeElements = envelope,
            OccupancySchedule = MapSchedule(room.OccupancySchedule),
            EquipmentSchedule = MapSchedule(room.EquipmentSchedule),
            LightingSchedule = MapSchedule(room.LightingSchedule)
        };
    }

    private static RoomCalculationScheduleSnapshot? MapSchedule(HourlySchedule? schedule)
    {
        if (schedule is null)
            return null;

        return new RoomCalculationScheduleSnapshot
        {
            ScheduleId = schedule.Id,
            Name = schedule.Name,
            Factors = schedule.Factors.ToArray()
        };
    }
}
