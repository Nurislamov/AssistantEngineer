using AssistantEngineer.Application.Contracts.Benchmarks;
using AssistantEngineer.Application.Contracts.Calculations;

namespace AssistantEngineer.Application.Services.Benchmarks;

public interface IVerificationComparator
{
    VerificationReport Compare(
        BuildingCalculationResult ourResult,
        EnergyPlusCalculationSummary epResult);
}