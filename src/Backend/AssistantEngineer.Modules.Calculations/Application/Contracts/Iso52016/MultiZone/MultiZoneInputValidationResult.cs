using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

public sealed record MultiZoneInputValidationResult(
    bool IsValid,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
