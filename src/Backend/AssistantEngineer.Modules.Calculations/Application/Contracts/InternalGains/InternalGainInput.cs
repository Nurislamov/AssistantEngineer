namespace AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;

public sealed record InternalGainInput(
    int RoomId,
    double? AreaM2 = null,

    int? OccupancyPeople = null,
    double? SensibleGainPerPersonW = null,
    double? LatentGainPerPersonW = null,

    double? LightingLoadW = null,
    double? LightingPowerDensityWPerM2 = null,

    double? EquipmentLoadW = null,
    double? EquipmentPowerDensityWPerM2 = null,

    double? ProcessSensibleGainW = null,
    double? ProcessLatentGainW = null,

    double? CustomSensibleGainW = null,
    double? CustomLatentGainW = null,

    double OccupancyScheduleFactor = 1.0,
    double LightingScheduleFactor = 1.0,
    double EquipmentScheduleFactor = 1.0,
    double ProcessScheduleFactor = 1.0,
    double CustomScheduleFactor = 1.0,

    string? DiagnosticsContext = null);