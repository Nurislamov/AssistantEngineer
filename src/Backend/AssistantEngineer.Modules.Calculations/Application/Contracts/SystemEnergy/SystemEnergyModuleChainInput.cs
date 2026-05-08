using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyModuleChainInput(
    string CalculationId,
    SystemEnergyUsefulLoadSet UsefulLoadSet,
    IReadOnlyList<SystemEnergyModuleInput> Modules,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source);
