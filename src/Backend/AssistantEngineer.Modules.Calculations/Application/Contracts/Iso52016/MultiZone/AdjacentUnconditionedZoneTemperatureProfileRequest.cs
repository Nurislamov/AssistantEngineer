namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record AdjacentUnconditionedZoneTemperatureProfileRequest(
    string ConditionId,
    IReadOnlyList<double> ConditionedZoneTemperatureProfileCelsius,
    IReadOnlyList<double> ExteriorTemperatureProfileCelsius,
    AdjacentUnconditionedTemperatureMode Mode,
    double? ReductionFactorB = null,
    double FallbackExteriorWeight = 0.7,
    double FallbackOffsetCelsius = 0.0);
