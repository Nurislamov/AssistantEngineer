using AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;
using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

namespace AssistantEngineer.Tests.Calculations.Governance;

public sealed class EngineeringCoreV2ReleaseReadinessServiceTests
{
    private readonly EngineeringCoreV2ReleaseReadinessService _service =
        new(
            new EngineeringStageManifestRegistryProvider(),
            new EngineeringStageManifestRegistryValidator(),
            new EngineeringClaimBoundaryScanner(),
            new EngineeringCalculationModeCatalogProvider());

    [Fact]
    public void Readiness_IsReadyOrReadyWithWarnings()
    {
        var result = _service.Evaluate(TestPaths.RepoRoot);

        Assert.True(
            result.ReadinessStatus is
                EngineeringGovernanceReleaseReadinessStatus.Ready or
                EngineeringGovernanceReleaseReadinessStatus.ReadyWithWarnings);
    }

    [Fact]
    public void Readiness_ContainsExternalValidationLimitationWarning()
    {
        var result = _service.Evaluate(TestPaths.RepoRoot);

        Assert.Contains(result.Diagnostics, item => item.Code == "Governance.ReleaseReadiness.ExternalValidationLimitation");
    }

    [Fact]
    public void OptInDefaults_AreFalse()
    {
        Assert.False(new NaturalVentilationOptions().UseIso16798InspiredCalculator);
        Assert.False(new Iso13370GroundHeatTransferOptions().UseIso13370InspiredBoundaryCalculator);
        Assert.False(new DomesticHotWaterOptions().UseIso12831InspiredCalculator);
        Assert.False(new SystemEnergyOptions().UseEn15316InspiredChain);
        Assert.False(new Iso52016ConstructionOptions().UseConstructionLayerMassInput);
    }

    [Fact]
    public void RollupCatalog_ContainsRequiredGovernanceModes()
    {
        var modeIds = new EngineeringCalculationModeCatalogProvider()
            .GetCatalog()
            .Select(mode => mode.ModeId)
            .ToArray();

        Assert.Contains("BUILDING-INPUT-VALIDATION-GOVERNANCE", modeIds);
        Assert.Contains("ENGINEERING-GOVERNANCE-STAGE-REGISTRY", modeIds);
        Assert.Contains("ENGINEERING-GOVERNANCE-CLAIM-SCANNER", modeIds);
        Assert.Contains("ENGINEERING-CORE-V2-RELEASE-READINESS", modeIds);
        Assert.Contains("ENGINEERING-CORPORATE-STATUS-SAMPLE", modeIds);
    }

    [Fact]
    public void BuiValidationStage_IsIncluded()
    {
        var registry = new EngineeringStageManifestRegistryProvider().BuildRegistry(TestPaths.RepoRoot);

        Assert.Contains("AE-BUI-VALIDATION-001", registry.StagesById.Keys);
    }

    [Fact]
    public void GeneratedArtifactsPolicy_DoesNotRequireCommittedArtifacts()
    {
        var result = _service.Evaluate(TestPaths.RepoRoot);

        Assert.DoesNotContain(result.Diagnostics, item => item.Code == "Governance.ReleaseReadiness.GeneratedArtifactOutsideIgnoredFolders");
    }
}
