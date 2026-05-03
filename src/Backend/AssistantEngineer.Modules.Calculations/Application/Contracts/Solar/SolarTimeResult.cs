namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;

public sealed record SolarTimeResult(
    int DayOfYear,
    double DecimalLocalHour,
    double FractionalYearRadians,
    double EquationOfTimeMinutes,
    double LongitudeCorrectionMinutes,
    double TimeOffsetMinutes,
    double TrueSolarTimeMinutes,
    double LocalSolarTimeHours,
    double HourAngleDegrees);
