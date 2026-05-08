using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGeneratorSet(
    string GeneratorSetId,
    SystemEnergyLoadSplitMode LoadSplitMode,
    IReadOnlyList<SystemEnergyGeneratorInput> Generators,
    StandardCalculationDisclosure? DisclosureOverride,
    string? Source,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
