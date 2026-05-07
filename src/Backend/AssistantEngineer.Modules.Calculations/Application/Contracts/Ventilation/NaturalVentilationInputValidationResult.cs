using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

public sealed record NaturalVentilationInputValidationResult(
    bool IsValid,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
