namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public sealed record EngineeringGovernanceStageManifest(
    string StageId,
    string Title,
    string Status,
    EngineeringGovernanceStageStatus StageStatus,
    EngineeringGovernanceStageKind StageKind,
    string ManifestPath,
    IReadOnlyList<EngineeringGovernanceStageDependency> DependsOn,
    EngineeringGovernanceClaimBoundary ClaimBoundary,
    IReadOnlyList<EngineeringGovernanceFileReference> ImplementationFiles,
    IReadOnlyList<EngineeringGovernanceFileReference> FixtureFiles,
    IReadOnlyList<EngineeringGovernanceFileReference> TestGuards,
    IReadOnlyList<EngineeringGovernanceFileReference> UpdatedDisclosureFiles,
    IReadOnlyList<EngineeringGovernanceFileReference> GeneratedArtifacts,
    string? ImplementationFilesReason = null,
    string? TestGuardsReason = null);
