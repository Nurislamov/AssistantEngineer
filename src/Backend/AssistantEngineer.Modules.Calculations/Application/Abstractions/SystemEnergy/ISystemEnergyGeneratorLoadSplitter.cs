using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyGeneratorLoadSplitter
{
    SystemEnergyGeneratorLoadSplitResult SplitLoads(
        SystemEnergyGenerationHandoff handoff,
        SystemEnergyGeneratorSet generatorSet);
}
