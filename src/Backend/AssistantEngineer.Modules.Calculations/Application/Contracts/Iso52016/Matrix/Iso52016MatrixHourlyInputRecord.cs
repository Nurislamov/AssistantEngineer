namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

/// <summary>
/// One hourly ISO 52016 Matrix solver input row.
/// Boundary temperatures are keyed by BoundaryId from Iso52016MatrixBoundaryConductance.
/// Node gains are keyed by NodeId and may include solar, internal gains and other sensible heat sources.
/// Boundary conductance overrides are optional and target declared (NodeId, BoundaryId) boundary links.
/// </summary>
public sealed record Iso52016MatrixHourlyInputRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    IReadOnlyDictionary<string, double> BoundaryTemperaturesC,
    IReadOnlyDictionary<string, double> NodeHeatGainsW,
    double? HeatingSetpointC = null,
    double? CoolingSetpointC = null,
    IReadOnlyList<Iso52016MatrixHourlyBoundaryConductanceOverride>? BoundaryConductanceOverrides = null);
