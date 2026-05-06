namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;

public sealed record BuildingInputValidationResult(
    BuildingInputValidationReadinessStatus ReadinessStatus,
    IReadOnlyList<BuildingInputValidationDiagnostic> Diagnostics,
    IReadOnlyDictionary<BuildingInputValidationSeverity, IReadOnlyList<BuildingInputValidationDiagnostic>> DiagnosticsBySeverity,
    IReadOnlyList<BuildingInputSuggestedCorrection> SuggestedCorrections,
    IReadOnlyList<string> ClaimBoundary)
{
    public int InfoCount => DiagnosticsBySeverity.TryGetValue(BuildingInputValidationSeverity.Info, out var values) ? values.Count : 0;
    public int WarningCount => DiagnosticsBySeverity.TryGetValue(BuildingInputValidationSeverity.Warning, out var values) ? values.Count : 0;
    public int ErrorCount => DiagnosticsBySeverity.TryGetValue(BuildingInputValidationSeverity.Error, out var values) ? values.Count : 0;
    public int CriticalCount => DiagnosticsBySeverity.TryGetValue(BuildingInputValidationSeverity.Critical, out var values) ? values.Count : 0;
}
