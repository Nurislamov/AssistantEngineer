using AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Governance;

public sealed class EngineeringCoreV2ReleaseReadinessService
{
    private static readonly IReadOnlyList<string> RequiredStageIds =
    [
        "AE-VALIDATION-ISO52016-001",
        "AE-VALIDATION-ISO52016-002",
        "AE-VALIDATION-standard-reference-001",
        "AE-VENT-001",
        "AE-VENT-002",
        "AE-GROUND-001",
        "AE-GROUND-002",
        "AE-DHW-001",
        "AE-DHW-002",
        "AE-EN15316-001",
        "AE-EN15316-002",
        "AE-CALC-ROLLUP-001",
        "AE-ISO52016-CONSTRUCTION-001",
        "AE-ISO52016-CONSTRUCTION-002",
        "AE-BUI-VALIDATION-001",
        "AE-GOVERNANCE-001",
        "AE-GOVERNANCE-002",
        "AE-RELEASE-READINESS-002",
        "AE-STATUS-001"
    ];

    private static readonly IReadOnlyList<string> RequiredRollupModes =
    [
        "VENTILATION-ISO16798-INSPIRED-OPTIN",
        "GROUND-ISO13370-INSPIRED-OPTIN",
        "DHW-ISO12831-INSPIRED-OPTIN",
        "SYSTEM-ENERGY-EN15316-INSPIRED-OPTIN",
        "ISO52016-CONSTRUCTION-LAYER-MASS-OPTIN",
        "BUILDING-INPUT-VALIDATION-GOVERNANCE",
        "ENGINEERING-GOVERNANCE-STAGE-REGISTRY",
        "ENGINEERING-GOVERNANCE-CLAIM-SCANNER",
        "ENGINEERING-CORE-V2-RELEASE-READINESS",
        "ENGINEERING-CORPORATE-STATUS-SAMPLE"
    ];

    private static readonly IReadOnlyList<string> RequiredDisclosureFiles =
    [
        "docs/calculations/ExternalReferenceValidationVerification.md",
        "docs/calculations/EngineeringCoreV1Scope.md",
        "docs/api/engineering-core-v1/status.sample.json",
        "docs/api/engineering-core-v1/calculation-mode-rollup.sample.json",
        "docs/api/engineering-core-v2/engineering-release-readiness.sample.json",
        "docs/calculations/governance/EngineeringCoreV2ReleaseReadiness.md"
    ];

    private static readonly IReadOnlyList<string> AllowedGeneratedArtifactPrefixes =
    [
        "artifacts/",
        "generated/",
        "TestResults/",
        "bin/",
        "obj/"
    ];

    private readonly EngineeringStageManifestRegistryProvider _registryProvider;
    private readonly EngineeringStageManifestRegistryValidator _registryValidator;
    private readonly EngineeringClaimBoundaryScanner _claimBoundaryScanner;
    private readonly EngineeringCalculationModeCatalogProvider _rollupCatalogProvider;

    public EngineeringCoreV2ReleaseReadinessService(
        EngineeringStageManifestRegistryProvider registryProvider,
        EngineeringStageManifestRegistryValidator registryValidator,
        EngineeringClaimBoundaryScanner claimBoundaryScanner,
        EngineeringCalculationModeCatalogProvider rollupCatalogProvider)
    {
        _registryProvider = registryProvider;
        _registryValidator = registryValidator;
        _claimBoundaryScanner = claimBoundaryScanner;
        _rollupCatalogProvider = rollupCatalogProvider;
    }

    public EngineeringGovernanceCheckResult Evaluate(string? repositoryRoot = null)
    {
        var registry = _registryProvider.BuildRegistry(repositoryRoot);
        var registryValidation = _registryValidator.Validate(registry);
        var claimScan = _claimBoundaryScanner.ScanRepository(repositoryRoot);

        var diagnostics = new List<EngineeringGovernanceCheckDiagnostic>();
        diagnostics.AddRange(registryValidation.Diagnostics);
        diagnostics.AddRange(claimScan.Diagnostics);

        var totalChecks = registryValidation.TotalChecks + claimScan.TotalChecks;
        var passedChecks = registryValidation.PassedChecks + claimScan.PassedChecks;

        totalChecks++;
        if (ValidateStageCoverage(registry, diagnostics))
            passedChecks++;

        totalChecks++;
        if (ValidateOptInSafety(diagnostics))
            passedChecks++;

        totalChecks++;
        if (ValidateRollupCatalog(diagnostics))
            passedChecks++;

        totalChecks++;
        if (ValidateBuildingInputValidationStage(registry, diagnostics))
            passedChecks++;

        totalChecks++;
        if (ValidateGeneratedArtifactPolicy(registry, diagnostics))
            passedChecks++;

        totalChecks++;
        if (ValidateDisclosureFiles(registry.RepositoryRoot, diagnostics))
            passedChecks++;

        totalChecks++;
        diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
            Code: "Governance.ReleaseReadiness.ExternalValidationLimitation",
            Severity: EngineeringGovernanceDiagnosticSeverity.Warning,
            Message: "External numerical validation remains limited; internal deterministic governance only."));

        var warningCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Warning);
        var errorCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Error);
        var criticalCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Critical);

        var readiness = criticalCount > 0 || errorCount > 0
            ? EngineeringGovernanceReleaseReadinessStatus.Blocked
            : warningCount > 0
                ? EngineeringGovernanceReleaseReadinessStatus.ReadyWithWarnings
                : EngineeringGovernanceReleaseReadinessStatus.Ready;

        var stageSummaries = registry.Stages
            .Select(stage => $"{stage.StageId}|{stage.Status}|{stage.ManifestPath}")
            .ToArray();

        return new EngineeringGovernanceCheckResult(
            CheckId: "EngineeringCoreV2ReleaseReadiness",
            ReadinessStatus: readiness,
            TotalChecks: totalChecks,
            PassedChecks: passedChecks,
            WarningCount: warningCount,
            ErrorCount: errorCount,
            CriticalCount: criticalCount,
            Diagnostics: diagnostics.OrderBy(item => item.Code, StringComparer.Ordinal).ToArray(),
            StageSummaries: stageSummaries);
    }

    private static bool ValidateStageCoverage(
        EngineeringGovernanceStageRegistry registry,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var missing = RequiredStageIds
            .Where(stageId => !registry.StagesById.ContainsKey(stageId))
            .ToArray();

        foreach (var stageId in missing)
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.RequiredStageMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: $"Required stage is missing from registry: {stageId}",
                StageId: stageId));
        }

        return missing.Length == 0;
    }

    private static bool ValidateOptInSafety(ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var defaults = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["NaturalVentilationOptions.UseIso16798InspiredCalculator"] = new NaturalVentilationOptions().UseIso16798InspiredCalculator,
            ["Iso13370GroundHeatTransferOptions.UseIso13370InspiredBoundaryCalculator"] = new Iso13370GroundHeatTransferOptions().UseIso13370InspiredBoundaryCalculator,
            ["DomesticHotWaterOptions.UseIso12831InspiredCalculator"] = new DomesticHotWaterOptions().UseIso12831InspiredCalculator,
            ["SystemEnergyOptions.UseEn15316InspiredChain"] = new SystemEnergyOptions().UseEn15316InspiredChain,
            ["Iso52016ConstructionOptions.UseConstructionLayerMassInput"] = new Iso52016ConstructionOptions().UseConstructionLayerMassInput
        };

        var passed = true;

        foreach (var (flagName, value) in defaults)
        {
            if (!value)
                continue;

            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.OptInFlagDefaultChanged",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: $"Opt-in option flag default must remain false: {flagName}",
                Token: flagName));
        }

        return passed;
    }

    private bool ValidateRollupCatalog(ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var catalog = _rollupCatalogProvider.GetCatalog();
        var availableModeIds = catalog.Select(mode => mode.ModeId).ToHashSet(StringComparer.Ordinal);
        var missingModeIds = RequiredRollupModes.Where(modeId => !availableModeIds.Contains(modeId)).ToArray();

        foreach (var modeId in missingModeIds)
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.RollupModeMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: $"Required rollup mode is missing: {modeId}",
                Token: modeId));
        }

        return missingModeIds.Length == 0;
    }

    private static bool ValidateBuildingInputValidationStage(
        EngineeringGovernanceStageRegistry registry,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        if (!registry.StagesById.TryGetValue("AE-BUI-VALIDATION-001", out var stage))
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.BuiStageMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "AE-BUI-VALIDATION-001 stage manifest is required for release readiness."));
            return false;
        }

        var boundaryText = string.Join('\n', stage.ClaimBoundary.Lines);
        var hasNoMutation = boundaryText.Contains("No automatic production data mutation", StringComparison.OrdinalIgnoreCase);
        var hasInternalOnly = boundaryText.Contains("Internal deterministic engineering governance only", StringComparison.OrdinalIgnoreCase);
        var hasDocs = stage.UpdatedDisclosureFiles.Count > 0;
        var hasTests = stage.TestGuards.Count > 0;

        var passed = hasNoMutation && hasInternalOnly && hasDocs && hasTests;
        if (!hasNoMutation)
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.BuiNoMutationClaimMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "AE-BUI-VALIDATION-001 must state no automatic production data mutation.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        if (!hasInternalOnly)
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.BuiInternalGovernanceClaimMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "AE-BUI-VALIDATION-001 must state internal governance-only scope.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        if (!hasDocs)
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.BuiDisclosureFilesMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "AE-BUI-VALIDATION-001 must include updated disclosure files.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        if (!hasTests)
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.BuiTestGuardsMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "AE-BUI-VALIDATION-001 must include test guards.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        return passed;
    }

    private static bool ValidateGeneratedArtifactPolicy(
        EngineeringGovernanceStageRegistry registry,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var passed = true;

        foreach (var stage in registry.Stages)
        {
            foreach (var generatedArtifact in stage.GeneratedArtifacts)
            {
                var normalized = generatedArtifact.Path.Replace('\\', '/');
                var allowed = AllowedGeneratedArtifactPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                if (allowed)
                    continue;

                passed = false;
                diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                    Code: "Governance.ReleaseReadiness.GeneratedArtifactOutsideIgnoredFolders",
                    Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                    Message: $"Generated artifact path must be ignored and not committed: {generatedArtifact.Path}",
                    FilePath: stage.ManifestPath,
                    StageId: stage.StageId));
            }
        }

        return passed;
    }

    private static bool ValidateDisclosureFiles(
        string repositoryRoot,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var passed = true;

        foreach (var path in RequiredDisclosureFiles)
        {
            var absolutePath = Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(absolutePath))
                continue;

            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.ReleaseReadiness.DisclosureFileMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: $"Required disclosure file is missing: {path}",
                FilePath: path));
        }

        return passed;
    }
}

