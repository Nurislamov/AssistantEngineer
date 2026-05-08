using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyUsefulLoadSet(
    string CalculationId,
    IReadOnlyList<SystemEnergyUsefulLoadInput> UsefulLoads,
    IReadOnlyList<SystemEnergyAuxiliaryLoadInput> AuxiliaryLoads,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source);
