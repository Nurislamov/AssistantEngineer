using AssistantEngineer.Modules.Buildings.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Buildings.Application.Contracts.Calculations;

public sealed class RoomCalculationInputSnapshot
{
    public int RoomId { get; init; }
    public int FloorId { get; init; }
    public int BuildingId { get; init; }
    public int ProjectId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public RoomTypeDto RoomType { get; init; }
    public double AreaM2 { get; init; }
    public double HeightM { get; init; }
    public double VolumeM3 { get; init; }
    public double IndoorTemperatureC { get; init; }
    public double? OutdoorTemperatureOverrideC { get; init; }
    public int PeopleCount { get; init; }
    public double EquipmentLoadW { get; init; }
    public double LightingLoadW { get; init; }
    public int? OccupancyScheduleId { get; init; }
    public int? EquipmentScheduleId { get; init; }
    public int? LightingScheduleId { get; init; }
    public RoomCalculationClimateInputSnapshot? Climate { get; init; }
    public RoomCalculationVentilationInputSnapshot? Ventilation { get; init; }
    public RoomCalculationGroundBoundaryInputSnapshot? GroundBoundary { get; init; }
    public IReadOnlyList<RoomCalculationWallSnapshot> Walls { get; init; } = [];
    public IReadOnlyList<RoomCalculationWindowSnapshot> Windows { get; init; } = [];
    public IReadOnlyList<RoomEnvelopeElementSnapshot> EnvelopeElements { get; init; } = [];
    public RoomCalculationScheduleSnapshot? OccupancySchedule { get; init; }
    public RoomCalculationScheduleSnapshot? EquipmentSchedule { get; init; }
    public RoomCalculationScheduleSnapshot? LightingSchedule { get; init; }
}

public sealed class RoomCalculationClimateInputSnapshot
{
    public int ClimateZoneId { get; init; }
    public string ClimateZoneName { get; init; } = string.Empty;
    public double SummerDesignTemperatureC { get; init; }
    public double WinterDesignTemperatureC { get; init; }
}

public sealed class RoomCalculationVentilationInputSnapshot
{
    public int VentilationParametersId { get; init; }
    public double AirChangesPerHour { get; init; }
    public double HeatRecoveryEfficiency { get; init; }
    public double InfiltrationAirChangesPerHour { get; init; }
    public double WindExposureFactor { get; init; }
    public double StackCoefficient { get; init; }
    public double WindCoefficient { get; init; }
}

public sealed class RoomCalculationGroundBoundaryInputSnapshot
{
    public GroundContactTypeDto ContactType { get; init; }
    public double ExposedPerimeterM { get; init; }
    public double BurialDepthM { get; init; }
    public double WallHeightBelowGradeM { get; init; }
    public double HorizontalInsulationWidthM { get; init; }
    public double PerimeterInsulationDepthM { get; init; }
    public double UnderfloorVentilationAirChangesPerHour { get; init; }
}

public sealed class RoomCalculationWallSnapshot
{
    public int WallId { get; init; }
    public double AreaM2 { get; init; }
    public double UValueWPerM2K { get; init; }
    public CardinalDirectionDto Orientation { get; init; }
    public WallBoundaryTypeDto BoundaryType { get; init; }
    public bool IsExternal { get; init; }
    public int? AdjacentRoomId { get; init; }
}

public sealed class RoomCalculationWindowSnapshot
{
    public int WindowId { get; init; }
    public double AreaM2 { get; init; }
    public double UValueWPerM2K { get; init; }
    public double Shgc { get; init; }
    public CardinalDirectionDto Orientation { get; init; }
    public double OverhangDepthM { get; init; }
    public double SideFinDepthM { get; init; }
    public double RevealDepthM { get; init; }
    public double WindowHeightM { get; init; }
    public double WindowWidthM { get; init; }
    public double MinimumDirectSolarReductionFactor { get; init; }
    public double DiffuseSolarShareUnaffected { get; init; }
}

public sealed class RoomEnvelopeElementSnapshot
{
    public string Kind { get; init; } = string.Empty;
    public int ElementId { get; init; }
    public double AreaM2 { get; init; }
    public double UValueWPerM2K { get; init; }
    public CardinalDirectionDto Orientation { get; init; }
}

public sealed class RoomCalculationScheduleSnapshot
{
    public int ScheduleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<double> Factors { get; init; } = [];
}
