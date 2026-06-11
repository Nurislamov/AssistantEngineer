using System.Text.Json;
using System.Diagnostics;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsManualIntakePipelineTests
{
    [Fact]
    public void CurrentRepositoryVerificationReportIsReleaseReadyForSeedConstraints()
    {
        var report = CreateService().Verify(CreateCurrentRepositoryInput());

        Assert.True(report.IsReleaseReady);
        Assert.False(report.HasBlockingIssues);
        Assert.True(report.RuntimeCatalog.TotalEntries >= 23);
        Assert.Empty(report.RuntimeCatalog.DuplicateKeys);
        Assert.Equal(0, report.RuntimeCatalog.ManualVerifiedEntries);
        Assert.Equal(report.RuntimeCatalog.TotalEntries, report.RuntimeCatalog.SeedEntries);
        Assert.Equal(GetDocsExampleFiles().Count, report.DocsExampleFileCount);
        Assert.Contains(report.Sections, section =>
            section.Name == "runtime-catalog" && !section.HasBlockingIssues);
        Assert.Contains(report.Sections, section =>
            section.Name == "docs-examples" && !section.HasBlockingIssues);
    }

    [Fact]
    public void InvalidStagingExampleIsDetectedButDoesNotBlockRelease()
    {
        var report = CreateService().Verify(CreateCurrentRepositoryInput());

        var summary = Assert.Single(report.CandidateSummaries, candidate =>
            candidate.SourceName.EndsWith(
                "gree-gmv-invalid-insufficient-evidence.sample.json",
                StringComparison.Ordinal));
        var examplesSection = Assert.Single(report.Sections, section => section.Name == "staging-examples");

        Assert.Equal(EquipmentDiagnosticsPromotionReadiness.Blocked, summary.Readiness);
        Assert.True(summary.ErrorCount > 0);
        Assert.Contains(examplesSection.Issues, issue =>
            issue.Code == "ManualVerifiedRequiresVerifiedEvidence");
        Assert.False(examplesSection.HasBlockingIssues);
        Assert.True(report.IsReleaseReady);
    }

    [Fact]
    public void ReadyForReviewStagingExampleIsClassifiedForEngineeringReview()
    {
        var report = CreateService().Verify(CreateCurrentRepositoryInput());

        var summary = Assert.Single(report.CandidateSummaries, candidate =>
            candidate.SourceName.EndsWith(
                "gree-gmv-ready-for-review.sample.json",
                StringComparison.Ordinal));

        Assert.Equal(EquipmentDiagnosticsPromotionReadiness.ReadyForEngineeringReview, summary.Readiness);
        Assert.Equal(0, summary.ErrorCount);
        Assert.Contains(summary.SuggestedNextActions, action =>
            action.Contains("engineering review", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(summary.SuggestedNextActions, action =>
            action.Contains("automatically", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VerificationReportDetectsDuplicateRuntimeKeys()
    {
        var entry = GetRuntimeEntries().First();
        var input = CreateSyntheticRuntimeInput([entry, entry]);

        var report = CreateService().Verify(input);

        Assert.False(report.IsReleaseReady);
        Assert.True(report.HasBlockingIssues);
        Assert.Single(report.RuntimeCatalog.DuplicateKeys);
        Assert.Contains(report.Sections.Single(section => section.Name == "runtime-catalog").Issues, issue =>
            issue.Code == "DuplicateRuntimeKey");
    }

    [Fact]
    public void VerificationReportDetectsUnsafeRuntimeWording()
    {
        var entry = GetRuntimeEntries().First() with
        {
            LikelyCauses = ["Unsafe synthetic bypass wording for verification test."]
        };
        var input = CreateSyntheticRuntimeInput([entry]);

        var report = CreateService().Verify(input);

        Assert.False(report.IsReleaseReady);
        Assert.Contains(report.Sections.Single(section => section.Name == "runtime-catalog").Issues, issue =>
            issue.Code == "UnsafeDiagnosticWording");
    }

    [Fact]
    public void VerificationReportDetectsManualVerifiedWithoutEvidence()
    {
        var original = GetRuntimeEntries().First();
        var entry = original with
        {
            Confidence = DiagnosticConfidence.ManualVerified,
            Source = original.Source with
            {
                SourceType = "SeededEngineeringKnowledge",
                EvidenceLevel = "UnverifiedSeed"
            }
        };
        var input = CreateSyntheticRuntimeInput([entry]);

        var report = CreateService().Verify(input);

        Assert.False(report.IsReleaseReady);
        Assert.Contains(report.Sections.Single(section => section.Name == "runtime-catalog").Issues, issue =>
            issue.Code == "ManualVerifiedRequiresVerifiedEvidence");
    }

    [Fact]
    public void DocsExamplesValidateWithoutRuntimePollution()
    {
        var input = CreateCurrentRepositoryInput();

        var report = CreateService().Verify(input);

        var docsSection = Assert.Single(report.Sections, section => section.Name == "docs-examples");
        Assert.Equal(GetDocsExampleFiles().Count, docsSection.FileCount);
        Assert.Empty(docsSection.Issues);
        Assert.DoesNotContain(input.RuntimeDocuments, document =>
            document.SourceName.Contains("docs/equipment-diagnostics/examples", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(input.RuntimeDocuments, document =>
            document.SourceName.Contains("/staging/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VerificationReportDetectsSyntheticRuntimeCatalogPollution()
    {
        var input = new EquipmentDiagnosticsVerificationInput(
            RuntimeEntries: GetRuntimeEntries(),
            RuntimeDocuments:
            [
                new EquipmentDiagnosticsVerificationDocument(
                    "src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Knowledge/staging/synthetic.json",
                    "{}",
                    EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate)
            ],
            StagingDocuments: [],
            DocsExampleDocuments: [],
            MinimumRuntimeCatalogCount: 0);

        var report = CreateService().Verify(input);

        Assert.False(report.IsReleaseReady);
        Assert.Contains(report.Sections.Single(section => section.Name == "runtime-catalog").Issues, issue =>
            issue.Code == "RuntimePollution");
    }

    [Fact]
    public void VerificationReportOutputIsDeterministic()
    {
        var service = CreateService();
        var input = CreateCurrentRepositoryInput();

        var first = JsonSerializer.Serialize(service.Verify(input));
        var second = JsonSerializer.Serialize(service.Verify(input));

        Assert.Equal(first, second);
    }

    [Fact]
    public void RealStagingCandidateWithPlaceholderEvidenceIsBlocked()
    {
        var readyExample = ReadDocument(
            GreeReadyForReviewSamplePath,
            EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate);
        var input = new EquipmentDiagnosticsVerificationInput(
            RuntimeEntries: GetRuntimeEntries(),
            RuntimeDocuments: GetRuntimeDocuments(),
            StagingDocuments: [readyExample],
            DocsExampleDocuments: [],
            MinimumRuntimeCatalogCount: 0);

        var report = CreateService().Verify(input);

        Assert.False(report.IsReleaseReady);
        Assert.Contains(report.Sections.Single(section => section.Name == "staging-candidates").Issues, issue =>
            issue.Code == "PlaceholderEvidenceInCandidate");
    }

    [Fact]
    public void ApprovedEvidenceBackedCandidateCanBeReadyForCatalogPromotionWithoutAutomaticWrite()
    {
        var input = new EquipmentDiagnosticsVerificationInput(
            RuntimeEntries: GetRuntimeEntries(),
            RuntimeDocuments: GetRuntimeDocuments(),
            StagingDocuments:
            [
                new EquipmentDiagnosticsVerificationDocument(
                    "synthetic/promotion-ready.json",
                    CreatePromotionReadyCandidateJson(),
                    EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate)
            ],
            DocsExampleDocuments: [],
            MinimumRuntimeCatalogCount: 0);

        var report = CreateService().Verify(input);

        var summary = Assert.Single(report.CandidateSummaries);
        Assert.Equal(EquipmentDiagnosticsPromotionReadiness.ReadyForCatalogPromotion, summary.Readiness);
        Assert.Equal(0, summary.ErrorCount);
        Assert.Contains(summary.SuggestedNextActions, action =>
            action.Contains("reviewed PR", StringComparison.OrdinalIgnoreCase));
        Assert.True(report.IsReleaseReady);
    }

    [Fact]
    public void VerificationScriptIsThinWrapperAroundDotnetTool()
    {
        var script = File.ReadAllText(VerificationScriptPath);

        Assert.Contains("dotnet run --project", script, StringComparison.Ordinal);
        Assert.Contains("AssistantEngineer.Tools.EquipmentDiagnosticsVerification", script, StringComparison.Ordinal);
        Assert.Contains("full-report", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-ChildItem", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConvertFrom-Json", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BranchReadinessScriptIsThinWrapperAroundDotnetTool()
    {
        var script = File.ReadAllText(BranchReadinessScriptPath);

        Assert.Contains("dotnet run --project", script, StringComparison.Ordinal);
        Assert.Contains("verify-branch", script, StringComparison.Ordinal);
        Assert.Contains("--base-ref", script, StringComparison.Ordinal);
        Assert.Contains("--scope", script, StringComparison.Ordinal);
        Assert.DoesNotContain("git diff", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Get-ChildItem", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Select-String", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BranchReadinessAllowsEquipmentDiagnosticsPathsAndReportsUntrackedState()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "docs/equipment-diagnostics/manual-intake-pipeline.md",
                    "Added",
                    false,
                    false,
                    false,
                    true,
                    "Manual intake documentation.")
            ]));

        Assert.True(report.Passed);
        Assert.Equal("Pass", report.Status);
        Assert.Equal(1, report.ChangedFilesSummary.Untracked);
        Assert.Equal(1, report.ChangedFilesSummary.Allowed);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void BranchReadinessBlocksCalculationsPath()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "src/Backend/AssistantEngineer.Modules.Calculations/Application/UnsafeChange.cs",
                    "Modified",
                    true,
                    false,
                    false,
                    false,
                    "namespace Synthetic;")
            ]));

        Assert.False(report.Passed);
        Assert.Equal("Fail", report.Status);
        Assert.Contains(report.Issues, issue => issue.Code == "ForbiddenChangedPath");
    }

    [Fact]
    public void BranchReadinessBlocksUnsafeUserFacingWording()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "docs/equipment-diagnostics/examples/unsafe.example.json",
                    "Added",
                    false,
                    false,
                    false,
                    true,
                    """{"instruction":"bypass the protection relay"}""")
            ]));

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Code == "UnsafeChangedWording");
    }

    [Fact]
    public void BranchReadinessAllowsExplicitNegativeTestContext()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "tests/AssistantEngineer.Tests/EquipmentDiagnostics/SyntheticSafetyTests.cs",
                    "Added",
                    false,
                    false,
                    false,
                    true,
                    """Assert.Contains("bypass", unsafeFragments);""")
            ]));

        Assert.True(report.Passed);
        Assert.DoesNotContain(report.Issues, issue => issue.Code == "UnsafeChangedWording");
    }

    [Fact]
    public void BranchReadinessAllowsValidationSchemaDenylistContext()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Knowledge/staging/synthetic.schema.json",
                    "Modified",
                    false,
                    false,
                    true,
                    false,
                    """{"not":{"pattern":"(?i)(bypass|disable protection|force run)"}}""")
            ]));

        Assert.True(report.Passed);
        Assert.DoesNotContain(report.Issues, issue => issue.Code == "UnsafeChangedWording");
    }

    [Fact]
    public void BranchReadinessAllowsExplicitShouldNotSafetyContext()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "docs/security/api-endpoint-protection-inventory.json",
                    "Modified",
                    false,
                    false,
                    true,
                    false,
                    """{"risk":"Endpoint should not bypass staged authorization controls."}""")
            ]));

        Assert.True(report.Passed);
        Assert.DoesNotContain(report.Issues, issue => issue.Code == "UnsafeChangedWording");
        Assert.DoesNotContain(report.Issues, issue => issue.Code == "SuspiciousChangedPath");
    }

    [Theory]
    [InlineData("src/Backend/AssistantEngineer.Api/Controllers/Equipment/EquipmentDiagnosticsController.cs")]
    [InlineData("tests/AssistantEngineer.Tests/Api/EquipmentDiagnosticBotApiIntegrationTests.cs")]
    [InlineData("docs/security/api-endpoint-protection-inventory.json")]
    [InlineData("docs/security/api-endpoint-protection-inventory.md")]
    public void BranchReadinessAllowsRequiredBotEndpointGovernancePaths(string path)
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(path, "Modified", false, false, true, false, "ED-15B governed change.")
            ]));

        Assert.True(report.Passed);
        Assert.Equal(1, report.ChangedFilesSummary.Allowed);
        Assert.Empty(report.Issues);
    }

    [Theory]
    [InlineData("src/Backend/AssistantEngineer.Modules.EquipmentDiagnostics/Application/Telegram/EquipmentDiagnosticTelegramAdapter.cs")]
    [InlineData("tests/AssistantEngineer.Tests/EquipmentDiagnostics/EquipmentDiagnosticTelegramAdapterTests.cs")]
    [InlineData("docs/equipment-diagnostics/telegram-adapter.md")]
    public void BranchReadinessNarrowlyAllowsTelegramAdapterSkeletonPaths(string path)
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(path, "Added", false, false, false, true, "Deterministic adapter skeleton.")
            ]));

        Assert.True(report.Passed);
        Assert.Equal(1, report.ChangedFilesSummary.Allowed);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void BranchReadinessStillBlocksTelegramTransportOutsideSkeletonAllowlist()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "src/Backend/AssistantEngineer.Api/Telegram/ProductionTransport.cs",
                    "Added",
                    false,
                    false,
                    false,
                    true,
                    "Synthetic transport.")
            ]));

        Assert.False(report.Passed);
        Assert.Contains(report.Issues, issue => issue.Code == "ForbiddenChangedPath");
    }

    [Fact]
    public void BranchReadinessFailsWhenCommandCheckFails()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [],
            [
                new BranchReadinessCommandResult(
                    "dotnet-test",
                    "dotnet test AssistantEngineer.sln --no-build",
                    false,
                    1,
                    "Tests failed.")
            ]));

        Assert.False(report.Passed);
        Assert.Equal(1, report.BlockersCount);
        Assert.Contains(report.NextActions, action =>
            action.Contains("failed build/test commands", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BranchReadinessReportOutputIsDeterministic()
    {
        var service = CreateBranchReadinessService();
        var input = CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "scripts/dev/verify-branch-readiness.ps1",
                    "Added",
                    false,
                    false,
                    false,
                    true,
                    "dotnet run")
            ]);

        var first = JsonSerializer.Serialize(service.Verify(input));
        var second = JsonSerializer.Serialize(service.Verify(input));

        Assert.Equal(first, second);
    }

    [Fact]
    public void PrBodyGeneratorRendersPassingReportDeterministicallyWithRequiredChecklist()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput([]));
        var generator = new BranchReadinessPrBodyGenerator();

        var first = generator.Render(report, "artifacts/verification/branch-readiness/branch-readiness-report.json");
        var second = generator.Render(report, "artifacts/verification/branch-readiness/branch-readiness-report.json");

        Assert.Equal(first, second);
        Assert.Contains("# EquipmentDiagnostics: equipment diagnostics manual intake pipeline", first, StringComparison.Ordinal);
        Assert.Contains("Readiness status: **PASS**", first, StringComparison.Ordinal);
        Assert.Contains("Allowed: `0`", first, StringComparison.Ordinal);
        Assert.Contains("Suspicious: `0`", first, StringComparison.Ordinal);
        Assert.Contains("Forbidden: `0`", first, StringComparison.Ordinal);
        Assert.Contains("- [x] No calculation physics changed.", first, StringComparison.Ordinal);
        Assert.Contains("- [x] No public calculation API route changed.", first, StringComparison.Ordinal);
        Assert.Contains("- [x] No EF/DB changes.", first, StringComparison.Ordinal);
        Assert.Contains("- [x] No Telegram integration.", first, StringComparison.Ordinal);
        Assert.Contains("- [x] No AI/RAG/vector search.", first, StringComparison.Ordinal);
        Assert.Contains("- [x] No staging/docs example runtime pollution.", first, StringComparison.Ordinal);
        Assert.Contains("- [x] Branch readiness report is PASS.", first, StringComparison.Ordinal);
        Assert.DoesNotContain("raw log", first, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrBodyGeneratorRendersFailReportWithBlockerSummary()
    {
        var report = CreateBranchReadinessService().Verify(CreateBranchInput(
            [
                new BranchReadinessFileInput(
                    "src/Backend/AssistantEngineer.Modules.Calculations/Synthetic.cs",
                    "Modified",
                    true,
                    false,
                    false,
                    false,
                    "namespace Synthetic;")
            ]));

        var body = new BranchReadinessPrBodyGenerator().Render(
            report,
            "artifacts/verification/branch-readiness/branch-readiness-report.json");

        Assert.Contains("Readiness status: **FAIL**", body, StringComparison.Ordinal);
        Assert.Contains("## Blockers", body, StringComparison.Ordinal);
        Assert.Contains("ForbiddenChangedPath", body, StringComparison.Ordinal);
        Assert.Contains("- [ ] Branch readiness report is PASS.", body, StringComparison.Ordinal);
    }

    [Fact]
    public void PreparePrBodyCommandFailsWhenReportIsMissing()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in new[]
                 {
                     "run",
                     "--project",
                     VerificationToolProjectPath,
                     "--",
                     "prepare-pr-body",
                     "--repo-root",
                     TestPaths.RepoRoot,
                     "--report",
                     "artifacts/verification/branch-readiness/missing-report.json"
                 })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);
        process.WaitForExit();

        Assert.NotEqual(0, process.ExitCode);
        Assert.Contains("not found", process.StandardError.ReadToEnd(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrAutomationScriptsRemainThinWrappers()
    {
        var prepareScript = File.ReadAllText(PreparePrBodyScriptPath);
        var combinedScript = File.ReadAllText(VerifyAndPreparePrScriptPath);

        Assert.Contains("prepare-pr-body", prepareScript, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertFrom-Json", prepareScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git diff", prepareScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("verify-branch-readiness.ps1", combinedScript, StringComparison.Ordinal);
        Assert.Contains("prepare-pr-body.ps1", combinedScript, StringComparison.Ordinal);
        Assert.DoesNotContain("ConvertFrom-Json", combinedScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git diff", combinedScript, StringComparison.OrdinalIgnoreCase);
    }

    private static EquipmentDiagnosticsVerificationService CreateService() => new();

    private static BranchReadinessVerificationService CreateBranchReadinessService() => new();

    private static BranchReadinessInput CreateBranchInput(
        IReadOnlyList<BranchReadinessFileInput> files,
        IReadOnlyList<BranchReadinessCommandResult>? commands = null) =>
        new(
            CurrentBranch: "feature/equipment-diagnostics-manual-intake-pipeline",
            BaseRef: "origin/master",
            Scope: "EquipmentDiagnostics",
            Files: files,
            EquipmentDiagnosticsReport: CreateService().Verify(CreateCurrentRepositoryInput()),
            Commands: commands ??
            [
                new BranchReadinessCommandResult(
                    "dotnet-test",
                    "dotnet test AssistantEngineer.sln --no-build",
                    true,
                    0,
                    "Passed.")
            ]);

    private static EquipmentDiagnosticsVerificationInput CreateCurrentRepositoryInput() =>
        new(
            RuntimeEntries: GetRuntimeEntries(),
            RuntimeDocuments: GetRuntimeDocuments(),
            StagingDocuments: GetStagingDocuments(),
            DocsExampleDocuments: GetDocsExampleFiles()
                .Select(path => ReadDocument(path, EquipmentDiagnosticsVerificationDocumentKind.DocsExample))
                .ToArray());

    private static EquipmentDiagnosticsVerificationInput CreateSyntheticRuntimeInput(
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> entries) =>
        new(
            RuntimeEntries: entries,
            RuntimeDocuments:
            [
                new EquipmentDiagnosticsVerificationDocument(
                    "synthetic/runtime.json",
                    "{}",
                    EquipmentDiagnosticsVerificationDocumentKind.RuntimeCatalog)
            ],
            StagingDocuments: [],
            DocsExampleDocuments: [],
            MinimumRuntimeCatalogCount: 0);

    private static IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> GetRuntimeEntries() =>
        new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

    private static IReadOnlyList<EquipmentDiagnosticsVerificationDocument> GetRuntimeDocuments() =>
        GetRuntimeFiles()
            .Select(path => ReadDocument(path, EquipmentDiagnosticsVerificationDocumentKind.RuntimeCatalog))
            .ToArray();

    private static IReadOnlyList<EquipmentDiagnosticsVerificationDocument> GetStagingDocuments() =>
        Directory.GetFiles(StagingRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => ReadDocument(path, GetStagingKind(path)))
            .ToArray();

    private static IReadOnlyList<string> GetRuntimeFiles() =>
        Directory.GetFiles(KnowledgeRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Where(path => !HasPathSegment(path, "staging"))
            .Where(path => !HasPathSegment(path, "manual-codebook"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> GetDocsExampleFiles() =>
        Directory.GetFiles(DocsExamplesRoot, "*.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static EquipmentDiagnosticsVerificationDocument ReadDocument(
        string path,
        EquipmentDiagnosticsVerificationDocumentKind kind) =>
        new(
            Path.GetRelativePath(TestPaths.RepoRoot, path).Replace('\\', '/'),
            File.ReadAllText(path),
            kind);

    private static EquipmentDiagnosticsVerificationDocumentKind GetStagingKind(string path) =>
        HasPathSegment(path, "templates")
            ? EquipmentDiagnosticsVerificationDocumentKind.StagingTemplate
            : HasPathSegment(path, "examples")
                ? EquipmentDiagnosticsVerificationDocumentKind.StagingExample
                : EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate;

    private static bool HasPathSegment(string path, string segment) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Contains(segment, StringComparer.OrdinalIgnoreCase);

    private static string CreatePromotionReadyCandidateJson() =>
        """
        {
          "candidates": [
            {
              "manufacturer": "Synthetic Manufacturer",
              "series": "Synthetic Series",
              "category": "SplitSystem",
              "modelCode": null,
              "code": "SYNTHETIC-42",
              "title": "Synthetic evidence-backed verification candidate",
              "meaning": "Synthetic candidate used only to verify promotion-readiness reporting.",
              "severity": "Service attention required",
              "proposedConfidence": "ManualVerified",
              "source": {
                "sourceType": "ServiceManual",
                "evidenceLevel": "ManualPageVerified",
                "manualTitle": "Synthetic verification manual",
                "manualVersion": null,
                "manualDocumentCode": null,
                "page": "42",
                "section": null,
                "quote": null,
                "notes": "Synthetic test evidence only.",
                "limitations": [
                  "Synthetic verification fixture only."
                ],
                "applicableModels": [],
                "applicableSeries": [
                  "Synthetic Series"
                ]
              },
              "likelyCauses": [
                "Synthetic cause used only for verification testing."
              ],
              "diagnosticSteps": [
                {
                  "order": 1,
                  "title": "Confirm synthetic fixture identity",
                  "instruction": "Record the synthetic fixture identifiers.",
                  "expectedResult": "Synthetic fixture identifiers are confirmed.",
                  "ifFailedAction": "Keep the synthetic candidate outside the runtime catalog."
                }
              ],
              "requiredMeasurements": [
                {
                  "name": "Synthetic reading",
                  "unit": "unit",
                  "description": "Synthetic measurement used only for verification testing.",
                  "requiredBeforeConclusion": true
                }
              ],
              "safetyNotes": [
                "Qualified technician review remains required."
              ],
              "tags": [
                "synthetic-test"
              ],
              "promotionNotes": [
                "Move to production catalog only in a reviewed PR after final validation."
              ],
              "reviewStatus": "ApprovedForCatalog"
            }
          ]
        }
        """;

    private static string ModuleRoot =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics");

    private static string KnowledgeRoot => Path.Combine(ModuleRoot, "Knowledge");

    private static string StagingRoot => Path.Combine(KnowledgeRoot, "staging");

    private static string DocsExamplesRoot =>
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "examples");

    private static string GreeReadyForReviewSamplePath =>
        Path.Combine(StagingRoot, "examples", "gree-gmv-ready-for-review.sample.json");

    private static string VerificationScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "equipment-diagnostics",
            "verify-equipment-diagnostics.ps1");

    private static string BranchReadinessScriptPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "scripts",
            "dev",
            "verify-branch-readiness.ps1");

    private static string PreparePrBodyScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "dev", "prepare-pr-body.ps1");

    private static string VerifyAndPreparePrScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "dev", "verify-and-prepare-pr.ps1");

    private static string VerificationToolProjectPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EquipmentDiagnosticsVerification",
            "AssistantEngineer.Tools.EquipmentDiagnosticsVerification.csproj");
}
