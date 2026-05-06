namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record Iso16798NaturalVentilationInput(
    double RoomVolumeM3,
    double IndoorTemperatureC,
    double OutdoorTemperatureC,
    double WindSpeedMPerS,
    double AirDensityKgPerM3,
    double AirSpecificHeatJPerKgK,
    double DischargeCoefficient,
    double MaximumAirChangesPerHour,
    double OpeningHeightM,
    double UsefulHeightDifferenceM,
    double WindPressureCoefficient,
    double WindExposureFactor,
    double StackCoefficient,
    double WindCoefficient,
    IReadOnlyList<Iso16798NaturalVentilationOpeningInput> Openings);
