namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneZoneHourlyProfile(
    string ZoneId,
    double InitialTemperatureCelsius,
    double ThermalCapacityJPerK,
    IReadOnlyList<double> HeatingSetpointProfileCelsius,
    IReadOnlyList<double> CoolingSetpointProfileCelsius,
    IReadOnlyList<double> InternalGainsProfileW,
    IReadOnlyList<double> SolarGainsProfileW,
    IReadOnlyList<double> VentilationInfiltrationConductanceProfileWPerK);
