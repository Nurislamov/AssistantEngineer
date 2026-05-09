namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyCalculationRequest(
    string CalculationId,
    IReadOnlyList<SystemEnergyUsefulLoadInput> LoadInputs,
    IReadOnlyList<SystemEnergyStageDefinition> StageDefinitions,
    IReadOnlyList<SystemEnergyGeneratorDefinition> GeneratorDefinitions,
    EnergyFactorCatalog FactorCatalog,
    double TimeStepHours,
    SystemEnergyProfileShape OutputResolution,
    SystemEnergyLossOwnershipPolicy OwnershipPolicy,
    bool StrictFactorMode = true);
