namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// One-hour diagnostics row for physical-to-Matrix request translation.
/// </summary>
public sealed record Iso52016PhysicalRoomModelHourlyDiagnostics(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    double SourceTotalGainsW,
    double DistributedNodeHeatGainsW,
    double NodeGainBalanceErrorW,
    int BoundaryConductanceOverrideCount,
    double MaxBoundaryConductanceOverrideWPerK);
