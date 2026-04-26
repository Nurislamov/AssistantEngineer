using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;

namespace AssistantEngineer.Modules.Benchmarks.Application.Abstractions;

internal interface IVerificationComparator
{
    VerificationReport Compare(
        BuildingCalculationResult ourResult,
        EnergyPlusCalculationSummary epResult);
}
