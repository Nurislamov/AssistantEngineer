namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyGenerationStageRequest(
    string CalculationId,
    SystemEnergyUseKind UseKind,
    IReadOnlyList<double> LoadToGenerationProfileKWh,
    IReadOnlyList<SystemEnergyGeneratorDefinition> Generators,
    double TimeStepHours,
    SystemEnergyLossOwnershipPolicy OwnershipPolicy);
