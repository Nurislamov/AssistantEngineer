using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;

public sealed record DomesticHotWaterDrawProfileResult(
    IReadOnlyList<double> HourlyFractions24,
    IReadOnlyList<double> MonthlyFractions12,
    IReadOnlyList<double> AnnualHourlyFractions8760,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
