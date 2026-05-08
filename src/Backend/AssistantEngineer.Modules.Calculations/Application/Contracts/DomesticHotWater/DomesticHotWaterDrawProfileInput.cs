using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDrawProfileInput(
    string ProfileId,
    IReadOnlyList<double>? HourlyFractions24,
    IReadOnlyList<double>? MonthlyFractions12,
    IReadOnlyList<double>? AnnualHourlyFractions8760,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
