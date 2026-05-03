using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016MonthlyResultsResponse(
    int BuildingId,
    string BuildingName,
    int Year,
    Iso52016CalculationTimeStepDto CalculationTimeStep,
    IReadOnlyList<Iso52016MonthlyEnergyNeed> MonthlyResults,
    double AnnualHeatingDemandKWh,
    double AnnualCoolingDemandKWh,
    Iso52016EnergyBalanceBreakdown Breakdown,
    IReadOnlyList<CalculationDiagnostic>? Diagnostics = null);
