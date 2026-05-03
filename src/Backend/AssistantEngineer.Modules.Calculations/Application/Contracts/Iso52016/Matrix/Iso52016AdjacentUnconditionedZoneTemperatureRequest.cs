namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;

public sealed record Iso52016AdjacentUnconditionedZoneTemperatureRequest(
    double OutdoorTemperatureC,
    double AdjacentZonePreviousTemperatureC,
    double ConditionedZoneTemperatureC,
    double HeatTransferToOutdoorWPerK,
    double HeatTransferToGroundWPerK,
    double GroundTemperatureC,
    double HeatTransferToConditionedZoneWPerK,
    double InternalGainsW,
    double SolarGainsW,
    double ThermalCapacityJPerK,
    double TimeStepSeconds = 3600.0);