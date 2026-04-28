namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

public sealed record SolarPositionResult(
    int DayOfYear,
    double SolarDeclinationDegrees,
    double EquationOfTimeMinutes,
    double HourAngleDegrees,
    double SolarAltitudeDegrees,
    double SolarAzimuthDegrees,
    double ZenithAngleDegrees,
    double RelativeAirMass);