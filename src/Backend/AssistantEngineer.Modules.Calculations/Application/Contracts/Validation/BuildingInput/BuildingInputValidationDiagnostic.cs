namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;

public sealed record BuildingInputValidationDiagnostic(
    string Code,
    BuildingInputValidationSeverity Severity,
    BuildingInputValidationCategory Category,
    BuildingInputValidationScope Scope,
    string TargetPath,
    string Message,
    BuildingInputSuggestedCorrection? SuggestedCorrection = null);
