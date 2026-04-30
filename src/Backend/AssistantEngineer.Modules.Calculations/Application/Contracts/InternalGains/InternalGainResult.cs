namespace AssistantEngineer.Modules.Calculations.Application.Contracts.InternalGains;

public sealed record InternalGainResult(
    int RoomId,

    double OccupancySensibleGainW,
    double OccupancyLatentGainW,

    double LightingGainW,
    double EquipmentGainW,

    double ProcessSensibleGainW,
    double ProcessLatentGainW,

    double CustomSensibleGainW,
    double CustomLatentGainW,

    double TotalSensibleGainW,
    double TotalLatentGainW,
    double TotalInternalGainW,

    double? AreaM2,
    int? OccupancyPeople,

    double OccupancyScheduleFactor,
    double LightingScheduleFactor,
    double EquipmentScheduleFactor,
    double ProcessScheduleFactor,
    double CustomScheduleFactor,

    IReadOnlyList<InternalGainDiagnostic> Diagnostics)
{
    public bool HasErrors =>
        Diagnostics.Any(diagnostic =>
            diagnostic.Severity == InternalGainDiagnosticSeverity.Error);
}