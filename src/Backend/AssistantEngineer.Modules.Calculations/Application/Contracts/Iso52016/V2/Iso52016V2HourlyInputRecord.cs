namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.V2;

/// <summary>
/// One hourly ISO 52016 V2 solver input row.
/// Boundary temperatures are keyed by BoundaryId from Iso52016V2BoundaryConductance.
/// Node gains are keyed by NodeId and may include solar, internal gains and other sensible heat sources.
/// </summary>
public sealed record Iso52016V2HourlyInputRecord(
    int HourOfYear,
    int Month,
    int Day,
    int Hour,
    IReadOnlyDictionary<string, double> BoundaryTemperaturesC,
    IReadOnlyDictionary<string, double> NodeHeatGainsW,
    double? HeatingSetpointC = null,
    double? CoolingSetpointC = null);