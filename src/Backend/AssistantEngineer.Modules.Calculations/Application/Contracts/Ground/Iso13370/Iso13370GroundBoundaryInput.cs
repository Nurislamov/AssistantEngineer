namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record Iso13370GroundBoundaryInput(
    double AreaM2,
    double ExposedPerimeterM,
    double GroundConductivityWPerMK,
    double FloorUValueWPerM2K,
    double IndoorAnnualMeanTemperatureC,
    double OutdoorAnnualMeanTemperatureC,
    IReadOnlyList<double>? OutdoorMonthlyMeanTemperaturesC,
    double GroundAnnualMeanTemperatureC,
    double GroundTemperatureAmplitudeC,
    double GroundTemperaturePhaseShiftMonths,
    double HorizontalInsulationWidthM,
    double PerimeterInsulationDepthM,
    double BurialDepthM,
    double WallHeightBelowGradeM,
    double UnderfloorVentilationAirChangesPerHour,
    Iso13370GroundContactKind ContactKind);
