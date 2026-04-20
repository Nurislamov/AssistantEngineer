using AssistantEngineer.Application.Contracts.Benchmarks;

namespace AssistantEngineer.Application.Abstractions;

public interface IEnergyPlusResultParser
{
    EnergyPlusCalculationSummary Parse(string outputDirectory);
}