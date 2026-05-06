namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;

public sealed record EngineeringCalculationMode(
    string ModeId,
    EngineeringCalculationModeDomain Domain,
    EngineeringCalculationModeKind Kind,
    EngineeringCalculationModeStatus Status,
    bool IsDefault,
    bool IsOptIn,
    string? OptionFlagName,
    IReadOnlyList<EngineeringCalculationModeStageStatus> Stages,
    IReadOnlyList<string> DocumentationFiles,
    IReadOnlyList<string> ManifestFiles,
    EngineeringCalculationModeClaimBoundary ClaimBoundary,
    EngineeringCalculationModeDisclosure Disclosure);
