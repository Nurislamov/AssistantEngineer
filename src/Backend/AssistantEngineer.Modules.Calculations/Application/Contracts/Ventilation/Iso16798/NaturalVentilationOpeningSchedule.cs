namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;

public sealed record NaturalVentilationOpeningSchedule(
    double OpeningFraction = 1.0,
    IReadOnlyList<double>? OpeningFractionProfile = null,
    int? HourIndex = null);
