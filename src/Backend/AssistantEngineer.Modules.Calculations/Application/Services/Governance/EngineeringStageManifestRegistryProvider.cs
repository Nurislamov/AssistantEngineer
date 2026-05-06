using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Governance;

public sealed class EngineeringStageManifestRegistryProvider
{
    private const string ReleaseDirectory = "docs/releases";

    private static readonly IReadOnlyList<RequiredStageDescriptor> RequiredStages =
    [
        new("ENGINEERING-CORE-V1", "docs/releases/EngineeringCoreV1Manifest.json", EngineeringGovernanceStageKind.FormulaGate, false),
        new("ISO52010-SOLAR-FOUNDATION-INTERNAL-GATE", "docs/releases/Iso52010SolarFoundationManifest.json", EngineeringGovernanceStageKind.InternalEngineeringGate, false),
        new("ISO52016-MATRIX-SOLVER", "docs/releases/Iso52016MatrixSolverStageManifest.json", EngineeringGovernanceStageKind.InternalEngineeringGate, false),

        new("AE-VALIDATION-ISO52016-001", "docs/releases/Iso52016ExternalNumericalValidationFrameworkManifest.json", EngineeringGovernanceStageKind.InternalValidationFramework, true),
        new("AE-VALIDATION-ISO52016-002", "docs/releases/Iso52016ManualIndependentNumericalFixturesManifest.json", EngineeringGovernanceStageKind.ManualValidationAnchor, true),
        new("AE-VALIDATION-PYBE-001", "docs/releases/Iso52016PyBuildingEnergyInspiredFixtureIntakeManifest.json", EngineeringGovernanceStageKind.MethodologyIntake, true),

        new("AE-VENT-001", "docs/releases/Iso16798NaturalVentilationStageManifest.json", EngineeringGovernanceStageKind.OptInCalculatorFoundation, true),
        new("AE-VENT-002", "docs/releases/Iso16798NaturalVentilationApplicationIntegrationManifest.json", EngineeringGovernanceStageKind.OptInApplicationIntegration, true),
        new("AE-GROUND-001", "docs/releases/Iso13370GroundBoundaryStageManifest.json", EngineeringGovernanceStageKind.OptInCalculatorFoundation, true),
        new("AE-GROUND-002", "docs/releases/Iso13370GroundBoundaryApplicationIntegrationManifest.json", EngineeringGovernanceStageKind.OptInApplicationIntegration, true),
        new("AE-DHW-001", "docs/releases/Iso12831DomesticHotWaterDemandStageManifest.json", EngineeringGovernanceStageKind.OptInCalculatorFoundation, true),
        new("AE-DHW-002", "docs/releases/Iso12831DomesticHotWaterApplicationIntegrationManifest.json", EngineeringGovernanceStageKind.OptInApplicationIntegration, true),
        new("AE-EN15316-001", "docs/releases/En15316SystemEnergyChainStageManifest.json", EngineeringGovernanceStageKind.OptInCalculatorFoundation, true),
        new("AE-EN15316-002", "docs/releases/En15316SystemEnergyApplicationIntegrationManifest.json", EngineeringGovernanceStageKind.OptInApplicationIntegration, true),
        new("AE-ISO52016-CONSTRUCTION-001", "docs/releases/Iso52016ConstructionLayerAndMassClassFoundationManifest.json", EngineeringGovernanceStageKind.OptInCalculatorFoundation, true),
        new("AE-ISO52016-CONSTRUCTION-002", "docs/releases/Iso52016ConstructionEnvelopeInputIntegrationManifest.json", EngineeringGovernanceStageKind.OptInApplicationIntegration, true),

        new("AE-CALC-ROLLUP-001", "docs/releases/EngineeringCalculationModeComparisonRollupManifest.json", EngineeringGovernanceStageKind.GovernanceAnchor, true),
        new("AE-BUI-VALIDATION-001", "docs/releases/BuildingInputValidationFrameworkManifest.json", EngineeringGovernanceStageKind.GovernanceAnchor, true),
        new("AE-GOVERNANCE-001", "docs/releases/EngineeringGovernanceStageManifestRegistryManifest.json", EngineeringGovernanceStageKind.GovernanceAnchor, true),
        new("AE-GOVERNANCE-002", "docs/releases/EngineeringClaimBoundaryScannerManifest.json", EngineeringGovernanceStageKind.GovernanceAnchor, true),
        new("AE-RELEASE-READINESS-002", "docs/releases/EngineeringCoreV2ReleaseReadinessManifest.json", EngineeringGovernanceStageKind.ReleaseReadinessGate, true),
        new("AE-STATUS-001", "docs/releases/EngineeringCorporateStatusSampleManifest.json", EngineeringGovernanceStageKind.StatusDisclosure, true)
    ];

    public EngineeringGovernanceStageRegistry BuildRegistry(string? repositoryRoot = null)
    {
        var repoRoot = ResolveRepositoryRoot(repositoryRoot);
        var diagnostics = new List<EngineeringGovernanceCheckDiagnostic>();

        var releaseDirectory = Path.Combine(repoRoot, ReleaseDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(releaseDirectory))
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Registry.ReleaseDirectoryMissing",
                Severity: EngineeringGovernanceDiagnosticSeverity.Critical,
                Message: $"Release manifest directory was not found: {releaseDirectory}",
                FilePath: NormalizeRelativePath(repoRoot, releaseDirectory)));

            return new EngineeringGovernanceStageRegistry(
                RepositoryRoot: repoRoot,
                Stages: Array.Empty<EngineeringGovernanceStageManifest>(),
                ManifestReferences: Array.Empty<EngineeringGovernanceManifestReference>(),
                RequiredStageReferences: BuildRequiredStageReferences(repoRoot, Array.Empty<EngineeringGovernanceStageManifest>()),
                Diagnostics: diagnostics);
        }

        var stages = new List<EngineeringGovernanceStageManifest>();
        var manifestReferences = new List<EngineeringGovernanceManifestReference>();

        foreach (var manifestPath in Directory.GetFiles(releaseDirectory, "*.json", SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
        {
            var relativeManifestPath = NormalizeRelativePath(repoRoot, manifestPath);
            var stage = TryReadStageManifest(repoRoot, manifestPath, diagnostics);
            if (stage is null)
                continue;

            stages.Add(stage);
            manifestReferences.Add(new EngineeringGovernanceManifestReference(
                StageId: stage.StageId,
                ManifestPath: relativeManifestPath,
                Exists: true,
                IsRequired: RequiredStages.Any(required => string.Equals(required.StageId, stage.StageId, StringComparison.Ordinal)),
                IsStrictRequired: RequiredStages.Any(required => string.Equals(required.StageId, stage.StageId, StringComparison.Ordinal) && required.IsStrictRequired)));
        }

        var engineeringCoreManifestPath = Path.Combine(repoRoot, "docs", "releases", "EngineeringCoreV1Manifest.json");
        if (File.Exists(engineeringCoreManifestPath) && !stages.Any(stage => string.Equals(stage.StageId, "ENGINEERING-CORE-V1", StringComparison.Ordinal)))
        {
            stages.Add(ReadEngineeringCoreV1PseudoStage(repoRoot, engineeringCoreManifestPath));
            manifestReferences.Add(new EngineeringGovernanceManifestReference(
                StageId: "ENGINEERING-CORE-V1",
                ManifestPath: NormalizeRelativePath(repoRoot, engineeringCoreManifestPath),
                Exists: true,
                IsRequired: true,
                IsStrictRequired: false));
        }

        var requiredReferences = BuildRequiredStageReferences(repoRoot, stages);

        foreach (var reference in requiredReferences.Where(item => !item.Exists))
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: reference.IsStrictRequired
                    ? "Governance.Registry.RequiredStageManifestMissing"
                    : "Governance.Registry.OptionalStageManifestMissing",
                Severity: reference.IsStrictRequired
                    ? EngineeringGovernanceDiagnosticSeverity.Warning
                    : EngineeringGovernanceDiagnosticSeverity.Info,
                Message: $"{(reference.IsStrictRequired ? "Required" : "Optional")} stage manifest '{reference.StageId}' was not found.",
                FilePath: reference.ManifestPath,
                StageId: reference.StageId));
        }

        return new EngineeringGovernanceStageRegistry(
            RepositoryRoot: repoRoot,
            Stages: stages.OrderBy(stage => stage.StageId, StringComparer.Ordinal).ToArray(),
            ManifestReferences: manifestReferences.OrderBy(reference => reference.StageId, StringComparer.Ordinal).ToArray(),
            RequiredStageReferences: requiredReferences,
            Diagnostics: diagnostics);
    }

    private static EngineeringGovernanceStageManifest? TryReadStageManifest(
        string repositoryRoot,
        string manifestPath,
        ICollection<EngineeringGovernanceCheckDiagnostic> diagnostics)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = document.RootElement;

            if (!root.TryGetProperty("stageId", out var stageIdElement) ||
                string.IsNullOrWhiteSpace(stageIdElement.GetString()))
            {
                return null;
            }

            var stageId = stageIdElement.GetString()!;
            var title = ReadString(root, "title") ?? stageId;
            var status = ReadString(root, "status") ?? "unknown";
            var stageStatus = MapStageStatus(status);
            var stageKind = InferStageKind(stageId);
            var relativeManifestPath = NormalizeRelativePath(repositoryRoot, manifestPath);

            var dependsOn = ReadStringArray(root, "dependsOn")
                .Select(dependency => new EngineeringGovernanceStageDependency(dependency))
                .ToArray();

            var claimBoundaryLines = ReadStringArray(root, "claimBoundary");
            var claimBoundary = new EngineeringGovernanceClaimBoundary(
                Lines: claimBoundaryLines,
                RequiredNonClaims: claimBoundaryLines.Where(line => line.Contains("No ", StringComparison.OrdinalIgnoreCase) || line.Contains("not ", StringComparison.OrdinalIgnoreCase)).ToArray());

            var implementationFiles = CreateFileReferences(repositoryRoot, ReadStringArray(root, "implementationFiles"));
            var fixtureFiles = CreateFileReferences(repositoryRoot, ReadStringArray(root, "fixtureFiles"));
            var testGuards = CreateFileReferences(repositoryRoot, ReadStringArray(root, "testGuards"));
            var updatedDisclosures = CreateFileReferences(repositoryRoot, ReadStringArray(root, "updatedDisclosureFiles"));
            var generatedArtifacts = CreateFileReferences(repositoryRoot, ReadStringArray(root, "generatedArtifacts"));

            return new EngineeringGovernanceStageManifest(
                StageId: stageId,
                Title: title,
                Status: status,
                StageStatus: stageStatus,
                StageKind: stageKind,
                ManifestPath: relativeManifestPath,
                DependsOn: dependsOn,
                ClaimBoundary: claimBoundary,
                ImplementationFiles: implementationFiles,
                FixtureFiles: fixtureFiles,
                TestGuards: testGuards,
                UpdatedDisclosureFiles: updatedDisclosures,
                GeneratedArtifacts: generatedArtifacts,
                ImplementationFilesReason: ReadString(root, "implementationFilesReason"),
                TestGuardsReason: ReadString(root, "testGuardsReason"));
        }
        catch (Exception exception)
        {
            diagnostics.Add(new EngineeringGovernanceCheckDiagnostic(
                Code: "Governance.Registry.ManifestParseFailed",
                Severity: EngineeringGovernanceDiagnosticSeverity.Error,
                Message: $"Manifest parse failed: {exception.Message}",
                FilePath: NormalizeRelativePath(repositoryRoot, manifestPath)));
            return null;
        }
    }

    private static EngineeringGovernanceStageManifest ReadEngineeringCoreV1PseudoStage(string repositoryRoot, string manifestPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var root = document.RootElement;

        var status = ReadString(root, "status") ?? "ClosedV1";
        var claimLines = ReadStringArray(root, "explicitNonClaims");

        return new EngineeringGovernanceStageManifest(
            StageId: "ENGINEERING-CORE-V1",
            Title: "Engineering Core V1 formula gate manifest",
            Status: status,
            StageStatus: EngineeringGovernanceStageStatus.ClosedInternalGate,
            StageKind: EngineeringGovernanceStageKind.FormulaGate,
            ManifestPath: NormalizeRelativePath(repositoryRoot, manifestPath),
            DependsOn: Array.Empty<EngineeringGovernanceStageDependency>(),
            ClaimBoundary: new EngineeringGovernanceClaimBoundary(claimLines, claimLines),
            ImplementationFiles: Array.Empty<EngineeringGovernanceFileReference>(),
            FixtureFiles: Array.Empty<EngineeringGovernanceFileReference>(),
            TestGuards: Array.Empty<EngineeringGovernanceFileReference>(),
            UpdatedDisclosureFiles: Array.Empty<EngineeringGovernanceFileReference>(),
            GeneratedArtifacts: Array.Empty<EngineeringGovernanceFileReference>(),
            ImplementationFilesReason: "Core V1 manifest is governance metadata only.",
            TestGuardsReason: "Core V1 test guards live in historical release-gate suites.");
    }

    private static IReadOnlyList<EngineeringGovernanceManifestReference> BuildRequiredStageReferences(
        string repositoryRoot,
        IReadOnlyCollection<EngineeringGovernanceStageManifest> stages)
    {
        return RequiredStages
            .Select(required =>
            {
                var stage = stages.FirstOrDefault(item => string.Equals(item.StageId, required.StageId, StringComparison.Ordinal));
                var expectedPath = NormalizeRelativePath(repositoryRoot, Path.Combine(repositoryRoot, required.ExpectedManifestPath.Replace('/', Path.DirectorySeparatorChar)));

                return new EngineeringGovernanceManifestReference(
                    StageId: required.StageId,
                    ManifestPath: stage?.ManifestPath ?? expectedPath,
                    Exists: stage is not null,
                    IsRequired: true,
                    IsStrictRequired: required.IsStrictRequired);
            })
            .OrderBy(item => item.StageId, StringComparer.Ordinal)
            .ToArray();
    }

    private static EngineeringGovernanceStageKind InferStageKind(string stageId)
    {
        return stageId switch
        {
            "AE-VALIDATION-ISO52016-001" => EngineeringGovernanceStageKind.InternalValidationFramework,
            "AE-VALIDATION-ISO52016-002" => EngineeringGovernanceStageKind.ManualValidationAnchor,
            "AE-VALIDATION-PYBE-001" => EngineeringGovernanceStageKind.MethodologyIntake,

            "AE-VENT-001" or "AE-GROUND-001" or "AE-DHW-001" or "AE-EN15316-001" or "AE-ISO52016-CONSTRUCTION-001"
                => EngineeringGovernanceStageKind.OptInCalculatorFoundation,

            "AE-VENT-002" or "AE-GROUND-002" or "AE-DHW-002" or "AE-EN15316-002" or "AE-ISO52016-CONSTRUCTION-002"
                => EngineeringGovernanceStageKind.OptInApplicationIntegration,

            "AE-CALC-ROLLUP-001" or "AE-BUI-VALIDATION-001" or "AE-GOVERNANCE-001" or "AE-GOVERNANCE-002"
                => EngineeringGovernanceStageKind.GovernanceAnchor,

            "AE-RELEASE-READINESS-002" => EngineeringGovernanceStageKind.ReleaseReadinessGate,
            "AE-STATUS-001" => EngineeringGovernanceStageKind.StatusDisclosure,

            _ when stageId.StartsWith("ISO52016-MATRIX", StringComparison.Ordinal)
                => EngineeringGovernanceStageKind.InternalEngineeringGate,
            _ when stageId.StartsWith("AE-ISO52016-002-STEP", StringComparison.Ordinal)
                => EngineeringGovernanceStageKind.InternalEngineeringGate,
            _ => EngineeringGovernanceStageKind.InternalEngineeringGate
        };
    }

    private static EngineeringGovernanceStageStatus MapStageStatus(string status)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "closed-internal-gate" => EngineeringGovernanceStageStatus.ClosedInternalGate,
            "closed" => EngineeringGovernanceStageStatus.ClosedInternalGate,
            "closedcandidate" => EngineeringGovernanceStageStatus.ClosedInternalGate,
            "closedv1" => EngineeringGovernanceStageStatus.ClosedInternalGate,
            "inprogress" => EngineeringGovernanceStageStatus.InternalGovernanceAnchor,
            "internal-engineering-gate" => EngineeringGovernanceStageStatus.ClosedInternalGate,
            "internal-engineering-anchor" => EngineeringGovernanceStageStatus.ClosedInternalGate,
            "internal-governance-anchor" => EngineeringGovernanceStageStatus.InternalGovernanceAnchor,
            "internal-validation-anchor" => EngineeringGovernanceStageStatus.InternalValidationAnchor,
            "internal-validation-framework" => EngineeringGovernanceStageStatus.InternalValidationAnchor,
            "manual-independent-validation-anchors" => EngineeringGovernanceStageStatus.InternalValidationAnchor,
            "methodology-alignment-fixture-intake" => EngineeringGovernanceStageStatus.InternalValidationAnchor,
            "validationanchersonly" => EngineeringGovernanceStageStatus.InternalValidationAnchor,
            "internal-application-integration-anchor" => EngineeringGovernanceStageStatus.InternalApplicationIntegrationAnchor,
            "internal-release-readiness-gate" => EngineeringGovernanceStageStatus.InternalReleaseReadinessGate,
            "internal-status-disclosure-anchor" => EngineeringGovernanceStageStatus.InternalStatusDisclosureAnchor,
            _ => EngineeringGovernanceStageStatus.Unknown
        };
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var valueElement) || valueElement.ValueKind != JsonValueKind.String)
            return null;

        return valueElement.GetString();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var valueElement) || valueElement.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return valueElement
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToArray();
    }

    private static IReadOnlyList<EngineeringGovernanceFileReference> CreateFileReferences(
        string repositoryRoot,
        IReadOnlyList<string> paths)
    {
        return paths
            .Select(path => path.Trim())
            .Where(path => path.Length > 0)
            .Select(path =>
            {
                var absolutePath = Path.GetFullPath(Path.Combine(repositoryRoot, path.Replace('/', Path.DirectorySeparatorChar)));
                return new EngineeringGovernanceFileReference(
                    Path: NormalizeRelativePath(repositoryRoot, absolutePath),
                    Exists: File.Exists(absolutePath) || Directory.Exists(absolutePath));
            })
            .ToArray();
    }

    private static string ResolveRepositoryRoot(string? explicitRepositoryRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRepositoryRoot))
            return Path.GetFullPath(explicitRepositoryRoot);

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AssistantEngineer.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing AssistantEngineer.sln was not found.");
    }

    private static string NormalizeRelativePath(string repositoryRoot, string absoluteOrRelativePath)
    {
        var fullRoot = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(absoluteOrRelativePath);

        if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(fullRoot.Length).Replace(Path.DirectorySeparatorChar, '/');

        return absoluteOrRelativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    private sealed record RequiredStageDescriptor(
        string StageId,
        string ExpectedManifestPath,
        EngineeringGovernanceStageKind StageKind,
        bool IsStrictRequired);
}
