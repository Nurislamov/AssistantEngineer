using AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Governance;

public sealed class EngineeringStageManifestRegistryValidator
{
    private static readonly string[] AllowedGeneratedArtifactPrefixes =
    [
        "artifacts/",
        "generated/",
        "TestResults/",
        "bin/",
        "obj/"
    ];

    public EngineeringGovernanceCheckResult Validate(EngineeringGovernanceStageRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var diagnostics = new List<EngineeringGovernanceCheckDiagnostic>();
        var totalChecks = 0;
        var passedChecks = 0;
        var strictStageIds = registry.RequiredStageReferences
            .Where(item => item.IsStrictRequired && item.Exists)
            .Select(item => item.StageId)
            .ToHashSet(StringComparer.Ordinal);

        totalChecks++;
        if (registry.RequiredStageReferences.Where(item => item.IsStrictRequired).All(item => item.Exists))
        {
            passedChecks++;
        }
        else
        {
            foreach (var requiredStage in registry.RequiredStageReferences.Where(item => item.IsStrictRequired && !item.Exists))
            {
                diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                    Code: "Governance.Registry.RequiredStageMissing",
                    Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                    Message: $"Required stage '{requiredStage.StageId}' does not have a manifest.",
                    FilePath: requiredStage.ManifestPath,
                    StageId: requiredStage.StageId));
            }
        }

        totalChecks++;
        var duplicateStageIds = registry.Stages
            .GroupBy(stage => stage.StageId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateStageIds.Length == 0)
        {
            passedChecks++;
        }
        else
        {
            foreach (var duplicateStageId in duplicateStageIds)
            {
                diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                    Code: "Governance.Registry.DuplicateStageId",
                    Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                    Message: $"Duplicate stage id found: {duplicateStageId}",
                    StageId: duplicateStageId));
            }
        }

        totalChecks++;
        var duplicateManifestPaths = registry.Stages
            .GroupBy(stage => stage.ManifestPath, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateManifestPaths.Length == 0)
        {
            passedChecks++;
        }
        else
        {
            foreach (var duplicatePath in duplicateManifestPaths)
            {
                diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                    Code: "Governance.Registry.DuplicateManifestPath",
                    Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                    Message: $"Duplicate manifest path found: {duplicatePath}",
                    FilePath: duplicatePath));
            }
        }

        foreach (var stage in registry.Stages)
        {
            var isStrictStage = strictStageIds.Contains(stage.StageId);

            totalChecks++;
            if (ValidateMandatoryManifestFields(stage, isStrictStage, diagnostics))
                passedChecks++;

            totalChecks++;
            if (ValidateFileReferences(stage, isStrictStage, diagnostics))
                passedChecks++;

            totalChecks++;
            if (ValidateDependencies(stage, registry, isStrictStage, diagnostics))
                passedChecks++;

            totalChecks++;
            if (ValidateGeneratedArtifacts(stage, diagnostics))
                passedChecks++;

            totalChecks++;
            if (ValidateClaimBoundary(stage, isStrictStage, diagnostics))
                passedChecks++;

            totalChecks++;
            if (ValidateKnownStatus(stage, isStrictStage, diagnostics))
                passedChecks++;
        }

        var warningCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Warning);
        var errorCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Error);
        var criticalCount = diagnostics.Count(item => item.Severity == EngineeringGovernanceDiagnosticSeverity.Critical);

        var readiness = criticalCount > 0 || errorCount > 0
            ? EngineeringGovernanceReleaseReadinessStatus.Blocked
            : warningCount > 0
                ? EngineeringGovernanceReleaseReadinessStatus.ReadyWithWarnings
                : EngineeringGovernanceReleaseReadinessStatus.Ready;

        return new EngineeringGovernanceCheckResult(
            CheckId: "EngineeringStageManifestRegistryValidation",
            ReadinessStatus: readiness,
            TotalChecks: totalChecks,
            PassedChecks: passedChecks,
            WarningCount: warningCount,
            ErrorCount: errorCount,
            CriticalCount: criticalCount,
            Diagnostics: registry.Diagnostics.Concat(diagnostics).OrderBy(item => item.Code, StringComparer.Ordinal).ToArray(),
            StageSummaries: registry.Stages.Select(stage => $"{stage.StageId}|{stage.Status}|{stage.ManifestPath}").ToArray());
    }

    private static bool ValidateMandatoryManifestFields(
        EngineeringGovernanceStageManifest stage,
        bool isStrictStage,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var passed = true;

        if (string.IsNullOrWhiteSpace(stage.StageId))
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.StageIdMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "Manifest does not define stageId.",
                FilePath: stage.ManifestPath));
        }

        if (string.IsNullOrWhiteSpace(stage.Title))
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.TitleMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "Manifest does not define title.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        if (string.IsNullOrWhiteSpace(stage.Status))
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.StatusMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "Manifest does not define status.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        if (stage.ClaimBoundary.Lines.Count == 0)
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.ClaimBoundaryMissing",
                Severity: isStrictStage ? EngineeringGovernanceDiagnosticSeverity.Error : EngineeringGovernanceDiagnosticSeverity.Warning,
                Message: "Manifest does not define claimBoundary entries.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        var hasImplementationDeclaration =
            stage.ImplementationFiles.Count > 0 ||
            !string.IsNullOrWhiteSpace(stage.ImplementationFilesReason);

        if (!hasImplementationDeclaration)
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.ImplementationFilesMissing",
                Severity: isStrictStage ? EngineeringGovernanceDiagnosticSeverity.Error : EngineeringGovernanceDiagnosticSeverity.Warning,
                Message: "Manifest must declare implementationFiles or implementationFilesReason.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        var hasTestGuardsDeclaration =
            stage.TestGuards.Count > 0 ||
            !string.IsNullOrWhiteSpace(stage.TestGuardsReason);

        if (!hasTestGuardsDeclaration)
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.TestGuardsMissing",
                Severity: isStrictStage ? EngineeringGovernanceDiagnosticSeverity.Error : EngineeringGovernanceDiagnosticSeverity.Warning,
                Message: "Manifest must declare testGuards or testGuardsReason.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        if (stage.GeneratedArtifacts is null)
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.GeneratedArtifactsMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "Manifest must declare generatedArtifacts.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        return passed;
    }

    private static bool ValidateFileReferences(
        EngineeringGovernanceStageManifest stage,
        bool isStrictStage,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var passed = true;

        passed &= ValidateReferenceSet(stage, stage.ImplementationFiles, "implementationFiles", isStrictStage, diagnostics);
        passed &= ValidateReferenceSet(stage, stage.FixtureFiles, "fixtureFiles", isStrictStage, diagnostics);
        passed &= ValidateReferenceSet(stage, stage.TestGuards, "testGuards", isStrictStage, diagnostics);
        passed &= ValidateReferenceSet(stage, stage.UpdatedDisclosureFiles, "updatedDisclosureFiles", isStrictStage, diagnostics);

        return passed;
    }

    private static bool ValidateReferenceSet(
        EngineeringGovernanceStageManifest stage,
        IReadOnlyList<EngineeringGovernanceFileReference> references,
        string bucket,
        bool isStrictStage,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var passed = true;

        foreach (var reference in references.Where(item => !item.Exists))
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.ReferencedFileMissing",
                Severity: isStrictStage ? EngineeringGovernanceDiagnosticSeverity.Error : EngineeringGovernanceDiagnosticSeverity.Warning,
                Message: $"Referenced file in '{bucket}' does not exist: {reference.Path}",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        return passed;
    }

    private static bool ValidateDependencies(
        EngineeringGovernanceStageManifest stage,
        EngineeringGovernanceStageRegistry registry,
        bool isStrictStage,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var passed = true;

        foreach (var dependency in stage.DependsOn)
        {
            if (registry.StagesById.ContainsKey(dependency.StageId))
                continue;

            if (dependency.IsExternalReference)
                continue;

            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.DependencyMissing",
                Severity: isStrictStage ? EngineeringGovernanceDiagnosticSeverity.Error : EngineeringGovernanceDiagnosticSeverity.Warning,
                Message: $"Dependency stage '{dependency.StageId}' is missing and not marked as external reference.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        return passed;
    }

    private static bool ValidateGeneratedArtifacts(
        EngineeringGovernanceStageManifest stage,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        var passed = true;

        foreach (var generatedArtifact in stage.GeneratedArtifacts)
        {
            var normalizedPath = generatedArtifact.Path.Replace('\\', '/');
            var allowed = AllowedGeneratedArtifactPrefixes.Any(prefix =>
                normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (allowed)
                continue;

            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.GeneratedArtifactPathNotIgnored",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: $"Generated artifact path must stay under ignored/generated directories: {generatedArtifact.Path}",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId));
        }

        return passed;
    }

    private static bool ValidateClaimBoundary(
        EngineeringGovernanceStageManifest stage,
        bool isStrictStage,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        if (!isStrictStage)
            return true;

        var passed = true;
        var claimText = string.Join('\n', stage.ClaimBoundary.Lines);

        var requiredSubjects = new[]
        {
            "StandardReference equivalence",
            "EnergyPlus comparison workflow",
            "ASHRAE 140 / BESTEST-style validation anchor",
            "external certification"
        };

        foreach (var subject in requiredSubjects)
        {
            if (ContainsNegatedClaim(claimText, subject))
                continue;

            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.ClaimBoundaryMissingRequiredNonClaim",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: $"Claim boundary must include a negated non-claim for '{subject}'.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId,
                Token: subject));
        }

        if (!ContainsNegatedClaim(claimText, "full ISO") && !ContainsNegatedClaim(claimText, "full EN") && !ContainsNegatedClaim(claimText, "full ISO/EN compliance"))
        {
            passed = false;
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Manifest.ClaimBoundaryMissingComplianceNonClaim",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: "Claim boundary must include a negated full ISO/EN compliance non-claim.",
                FilePath: stage.ManifestPath,
                StageId: stage.StageId,
                Token: "full ISO/EN compliance"));
        }

        return passed;
    }

    private static bool ValidateKnownStatus(
        EngineeringGovernanceStageManifest stage,
        bool isStrictStage,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        if (stage.StageStatus != EngineeringGovernanceStageStatus.Unknown)
            return true;

        diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
            Code: "Governance.Manifest.UnknownStatus",
            Severity: isStrictStage ? EngineeringGovernanceDiagnosticSeverity.Error : EngineeringGovernanceDiagnosticSeverity.Warning,
            Message: $"Manifest status is unknown or unsupported: '{stage.Status}'.",
            FilePath: stage.ManifestPath,
            StageId: stage.StageId));

        return false;
    }

    private static bool ContainsNegatedClaim(string text, string subject)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(subject, StringComparison.OrdinalIgnoreCase))
                continue;

            var normalized = line.ToLowerInvariant();
            if (normalized.Contains("no ") ||
                normalized.Contains("not ") ||
                normalized.Contains("must not") ||
                normalized.Contains("without") ||
                normalized.Contains("is not") ||
                normalized.Contains("are not"))
            {
                return true;
            }
        }

        return false;
    }
}
