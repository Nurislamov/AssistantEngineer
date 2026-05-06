using AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Calculations.Governance;

public sealed class EngineeringStageManifestRegistryValidatorTests
{
    private readonly EngineeringStageManifestRegistryProvider _provider = new();
    private readonly EngineeringStageManifestRegistryValidator _validator = new();

    [Fact]
    public void ValidRegistry_PassesWithoutErrors()
    {
        var registry = _provider.BuildRegistry(TestPaths.RepoRoot);

        var result = _validator.Validate(registry);

        Assert.True(result.ErrorCount == 0, string.Join('\n', result.Diagnostics.Select(item => item.Message)));
        Assert.True(result.CriticalCount == 0, string.Join('\n', result.Diagnostics.Select(item => item.Message)));
    }

    [Fact]
    public void MissingRequiredManifest_ProducesError()
    {
        var original = _provider.BuildRegistry(TestPaths.RepoRoot);
        var trimmedStages = original.Stages.Where(stage => stage.StageId != "AE-GOVERNANCE-001").ToArray();
        var requiredReferences = original.RequiredStageReferences
            .Select(reference => reference.StageId == "AE-GOVERNANCE-001"
                ? reference with { Exists = false }
                : reference)
            .ToArray();

        var registry = new EngineeringGovernanceStageRegistry(
            RepositoryRoot: original.RepositoryRoot,
            Stages: trimmedStages,
            ManifestReferences: original.ManifestReferences,
            RequiredStageReferences: requiredReferences,
            Diagnostics: original.Diagnostics);

        var result = _validator.Validate(registry);

        Assert.Contains(result.Diagnostics, item =>
            item.Code == "Governance.Registry.RequiredStageMissing" &&
            item.StageId == "AE-GOVERNANCE-001");
    }

    [Fact]
    public void MissingImplementationFile_ProducesError()
    {
        var original = _provider.BuildRegistry(TestPaths.RepoRoot);
        var stage = original.Stages.First(item => item.StageId == "AE-GOVERNANCE-001");

        var brokenStage = stage with
        {
            ImplementationFiles = [new EngineeringGovernanceFileReference("src/Missing/File.cs", false)]
        };

        var registry = ReplaceStage(original, brokenStage);
        var result = _validator.Validate(registry);

        Assert.Contains(result.Diagnostics, item =>
            item.Code == "Governance.Manifest.ReferencedFileMissing" &&
            item.StageId == stage.StageId);
    }

    [Fact]
    public void DuplicateStageId_ProducesError()
    {
        var original = _provider.BuildRegistry(TestPaths.RepoRoot);
        var duplicate = original.Stages.First(item => item.StageId == "AE-GOVERNANCE-002");

        var registry = new EngineeringGovernanceStageRegistry(
            RepositoryRoot: original.RepositoryRoot,
            Stages: original.Stages.Concat([duplicate]).ToArray(),
            ManifestReferences: original.ManifestReferences,
            RequiredStageReferences: original.RequiredStageReferences,
            Diagnostics: original.Diagnostics);

        var result = _validator.Validate(registry);

        Assert.Contains(result.Diagnostics, item => item.Code == "Governance.Registry.DuplicateStageId");
    }

    [Fact]
    public void MissingClaimBoundary_ProducesError()
    {
        var original = _provider.BuildRegistry(TestPaths.RepoRoot);
        var stage = original.Stages.First(item => item.StageId == "AE-GOVERNANCE-002");

        var brokenStage = stage with
        {
            ClaimBoundary = new EngineeringGovernanceClaimBoundary(Array.Empty<string>(), Array.Empty<string>())
        };

        var registry = ReplaceStage(original, brokenStage);
        var result = _validator.Validate(registry);

        Assert.Contains(result.Diagnostics, item =>
            item.Code == "Governance.Manifest.ClaimBoundaryMissing" &&
            item.StageId == stage.StageId);
    }

    private static EngineeringGovernanceStageRegistry ReplaceStage(
        EngineeringGovernanceStageRegistry registry,
        EngineeringGovernanceStageManifest replacement)
    {
        var stages = registry.Stages
            .Select(stage => stage.StageId == replacement.StageId ? replacement : stage)
            .ToArray();

        return new EngineeringGovernanceStageRegistry(
            RepositoryRoot: registry.RepositoryRoot,
            Stages: stages,
            ManifestReferences: registry.ManifestReferences,
            RequiredStageReferences: registry.RequiredStageReferences,
            Diagnostics: registry.Diagnostics);
    }
}
