namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;

/// <summary>
/// Deterministic diagnostics for an ISO52016-inspired physical room model after it has been translated to a Matrix request.
/// This is an internal engineering inspection contract, not an external numerical validation claim.
/// </summary>
public sealed record Iso52016PhysicalRoomModelDiagnosticsProfile(
    string ZoneCode,
    string AirNodeId,
    int NodeCount,
    int InternalConductanceLinkCount,
    int BoundaryConductanceLinkCount,
    int HourCount,
    double TotalHeatCapacityJPerK,
    double TotalInternalConductanceWPerK,
    double TotalBoundaryConductanceWPerK,
    IReadOnlyList<string> NodeIds,
    IReadOnlyList<string> BoundaryIds,
    IReadOnlyList<Iso52016PhysicalRoomModelHourlyDiagnostics> Hours)
{
    public double MaxAbsoluteNodeGainBalanceErrorW =>
        Hours.Count == 0
            ? 0.0
            : Hours.Max(hour => Math.Abs(hour.NodeGainBalanceErrorW));

    public double MaxBoundaryConductanceOverrideWPerK =>
        Hours.Count == 0
            ? 0.0
            : Hours.Max(hour => hour.MaxBoundaryConductanceOverrideWPerK);
}
