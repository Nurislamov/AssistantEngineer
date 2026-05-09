namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record SystemEnergyStageCalculationRequest(
    string CalculationId,
    SystemEnergyUseKind UseKind,
    IReadOnlyList<double> InputProfileKWh,
    SystemEnergyStageDefinition StageDefinition,
    double TimeStepHours,
    SystemEnergyLossOwnershipPolicy OwnershipPolicy);
