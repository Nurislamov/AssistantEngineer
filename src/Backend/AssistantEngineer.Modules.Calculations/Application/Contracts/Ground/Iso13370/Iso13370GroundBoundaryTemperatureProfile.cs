namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ground.Iso13370;

public sealed record Iso13370GroundBoundaryTemperatureProfile(
    IReadOnlyList<Iso13370MonthlyGroundBoundaryRecord> MonthlyRecords,
    double AnnualMeanBoundaryTemperatureC);
