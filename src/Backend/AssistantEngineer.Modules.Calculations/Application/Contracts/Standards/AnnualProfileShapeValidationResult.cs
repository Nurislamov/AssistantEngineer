namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

public sealed record AnnualProfileShapeValidationResult(
    bool IsValid,
    int ExpectedCount,
    int ActualCount,
    IReadOnlyList<StandardCalculationDiagnostic> Diagnostics);
