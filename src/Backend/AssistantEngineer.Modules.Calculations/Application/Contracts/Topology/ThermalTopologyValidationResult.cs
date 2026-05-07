using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;

public sealed record ThermalTopologyValidationResult(
    bool IsValid,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
