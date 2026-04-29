namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record VentilationAndInfiltrationLoadInput(
    int RoomId,
    double AreaM2,
    double VolumeM3,
    int OccupancyPeople,
    double IndoorTemperatureC,
    double OutdoorTemperatureC,
    double? MechanicalAirflowM3PerHour = null,
    double? AirflowLitersPerSecond = null,
    double? AirflowPerPersonLps = null,
    double? AirflowPerAreaLpsM2 = null,
    double? AirChangesPerHour = null,
    double? InfiltrationAirChangesPerHour = null,
    double? InfiltrationAirflowM3PerHour = null,
    double? NaturalVentilationAirflowM3PerHour = null,
    double? HeatRecoveryEfficiency = null,
    double ScheduleFactor = 1.0,
    VentilationLoadCalculationMode CalculationMode = VentilationLoadCalculationMode.DesignPoint,
    double? AirDensityKgPerM3 = null,
    double? AirSpecificHeatJPerKgK = null,
    string? DiagnosticsContext = null);
