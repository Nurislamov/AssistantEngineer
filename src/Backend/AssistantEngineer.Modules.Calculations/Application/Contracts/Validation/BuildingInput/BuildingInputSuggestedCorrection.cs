namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;

public sealed record BuildingInputSuggestedCorrection(
    string CorrectionId,
    string TargetPath,
    string Description,
    string? ProposedValue,
    bool IsAutomaticSafe,
    bool RequiresUserReview);
