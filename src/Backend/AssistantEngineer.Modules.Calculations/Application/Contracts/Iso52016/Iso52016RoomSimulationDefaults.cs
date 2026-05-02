namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016RoomSimulationDefaults(
    double FrameFraction = 0.25,
    double DefaultSolarHeatGainCoefficient = 0.6,
    double DefaultSensibleHeatGainPerPersonW = 125.0,
    double DefaultAirChangesPerHour = 0.5,
    double DefaultHeatRecoveryEfficiency = 0.0,
    double AirHeatCapacityWhPerM3K = 0.34,
    double FloorHeatCapacityKjPerM2K = 50.0,
    double CeilingHeatCapacityKjPerM2K = 50.0,
    double FallbackInternalHeatCapacityKjPerM2K = 165.0,
    double DefaultHeatingSetpointC = 20.0,
    double DefaultCoolingSetpointC = 26.0);