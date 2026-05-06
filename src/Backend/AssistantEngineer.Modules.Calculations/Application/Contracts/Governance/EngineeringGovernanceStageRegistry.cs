namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public sealed record EngineeringGovernanceStageRegistry(
    string RepositoryRoot,
    IReadOnlyList<EngineeringGovernanceStageManifest> Stages,
    IReadOnlyList<EngineeringGovernanceManifestReference> ManifestReferences,
    IReadOnlyList<EngineeringGovernanceManifestReference> RequiredStageReferences,
    IReadOnlyList<EngineeringGovernanceCheckDiagnostic> Diagnostics)
{
    public IReadOnlyDictionary<string, EngineeringGovernanceStageManifest> StagesById { get; } =
        Stages
            .GroupBy(stage => stage.StageId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
}
