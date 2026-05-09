namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;

public sealed record HeatingOperatingCondition(
    string ConditionId,
    FlowReturnTemperaturePair FlowReturnTemperatureC,
    double? OutdoorTemperatureC = null,
    double? OutdoorResetSlopePerK = null,
    double? OutdoorResetReferenceTemperatureC = null);
