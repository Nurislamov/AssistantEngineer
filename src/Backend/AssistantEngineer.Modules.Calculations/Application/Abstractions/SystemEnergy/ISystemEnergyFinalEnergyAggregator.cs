using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;

public interface ISystemEnergyFinalEnergyAggregator
{
    SystemEnergyFinalEnergyResult Aggregate(
        string calculationId,
        SystemEnergyGenerationHandoff handoff,
        IReadOnlyList<SystemEnergyGeneratorResult> generatorResults,
        StandardCalculationDisclosure? disclosureOverride);
}
