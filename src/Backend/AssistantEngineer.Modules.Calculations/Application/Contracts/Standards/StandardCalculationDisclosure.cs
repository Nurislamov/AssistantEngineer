namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

public sealed record StandardCalculationDisclosure(
    StandardCalculationFamily Family,
    StandardCalculationStage Stage,
    StandardCalculationMode Mode,
    string CalculationPath,
    bool IsFallback,
    bool UsesExternalValidation,
    StandardClaimBoundary ClaimBoundary,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
