using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;

public sealed record Iso52016HourlyResultsResponse(
    int BuildingId,
    string BuildingName,
    int Year,
    int? MonthFilter,
    int HourCount,
    Iso52016CalculationTimeStepDto CalculationTimeStep,
    IReadOnlyList<Iso52016HourlyEnergyNeed> HourlyResults);