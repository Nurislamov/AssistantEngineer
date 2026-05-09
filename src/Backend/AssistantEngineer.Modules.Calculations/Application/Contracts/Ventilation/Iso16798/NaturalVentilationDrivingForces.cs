namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record NaturalVentilationDrivingForces(
    double IndoorTemperatureC,
    double OutdoorTemperatureC,
    double WindSpeedMPerS,
    double? OpeningHeightM = null,
    double? AirDensityKgPerM3 = null,
    double? AirSpecificHeatJPerKgK = null,
    double? AltitudeMeters = null);
