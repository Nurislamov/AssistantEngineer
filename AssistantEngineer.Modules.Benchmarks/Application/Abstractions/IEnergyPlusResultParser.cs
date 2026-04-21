using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;

namespace AssistantEngineer.Modules.Benchmarks.Application.Abstractions;

public interface IEnergyPlusResultParser
{
    EnergyPlusCalculationSummary Parse(string outputDirectory);
}