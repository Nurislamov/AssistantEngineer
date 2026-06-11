using System.Diagnostics;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticsEvidenceRulesTests
{
    [Fact]
    public void CompletePrimaryTroubleshootingEvidenceForE1E3E4IsReadyAndProducesDeterministicSafePreview()
    {
        var input = CreateInput(
            [
                Occurrence("E1", "Protection", "TroubleshootingSection", "Outdoor", safety: true),
                Occurrence("E3", "Protection", "TroubleshootingSection", "Outdoor", safety: true),
                Occurrence("E4", "Protection", "TroubleshootingSection", "Outdoor", safety: true)
            ],
            usages: new Dictionary<string, string> { ["manual-1"] = "PrimaryTroubleshootingSource" });
        var coverage = new EquipmentDiagnosticsCodebookCoverageAnalyzer().Analyze(input);
        var engine = new EquipmentDiagnosticsEvidenceRuleEngine();

        var first = engine.Assess(input, coverage);
        var second = engine.Assess(input, coverage);
        var preview = new EquipmentDiagnosticsStagingPreviewGenerator().Generate(first);

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.All(first.Assessments, value =>
        {
            Assert.Equal(EquipmentDiagnosticsEvidenceAssessmentStatus.ReadyForStagingCandidate, value.Status);
            Assert.Equal(EquipmentDiagnosticsEvidenceConfidenceBucket.StrongManualEvidence, value.ConfidenceBucket);
        });
        Assert.Equal(new[] { "E1", "E3", "E4" }, preview.CandidateCodes);
        Assert.Equal(3, preview.CandidateCount);
        Assert.All(preview.Candidates, value => Assert.Equal("DraftPreview", value.ReviewStatus));
        Assert.DoesNotContain("ApprovedForCatalog", JsonSerializer.Serialize(preview), StringComparison.Ordinal);
        Assert.All(UnsafeFragments, fragment => Assert.DoesNotContain(fragment, JsonSerializer.Serialize(preview), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PartialAndSupportingSourcesAreNotReady()
    {
        var input = CreateInput(
            [
                Occurrence("E1", "Protection", "ErrorIndicationTable", "Outdoor", safety: true, manualId: "primary"),
                Occurrence("E3", "Protection", "TroubleshootingSection", "Outdoor", safety: true, manualId: "owner"),
                Occurrence("GUIDE", "Unknown", "TechnicalGuideApplicability", "TechnicalGuide", manualId: "guide"),
                Occurrence("TOOL", "ToolFunction", "ControllerOperationSection", "CommissioningTool", manualId: "tool"),
                Occurrence("A0", "Status", "DebuggingProcedure", "System", manualId: "primary"),
                Occurrence("n6", "Query", "DebuggingProcedure", "System", manualId: "primary"),
                Occurrence("qA", "Setting", "DebuggingProcedure", "System", manualId: "primary"),
                Occurrence("db", "Debugging", "DebuggingProcedure", "System", manualId: "primary")
            ],
            usages: new Dictionary<string, string>
            {
                ["primary"] = "PrimaryTroubleshootingSource",
                ["owner"] = "OwnerReferenceSource",
                ["guide"] = "TechnicalApplicabilitySource",
                ["tool"] = "CommissioningToolSource"
            });

        var report = new EquipmentDiagnosticsEvidenceRuleEngine().Assess(input, new EquipmentDiagnosticsCodebookCoverageAnalyzer().Analyze(input));

        Assert.Equal(EquipmentDiagnosticsEvidenceAssessmentStatus.NeedsTroubleshootingSection,
            report.Assessments.Single(value => value.Code == "E1").Status);
        Assert.Equal(EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByMissingManualEvidence,
            report.Assessments.Single(value => value.Code == "E3").Status);
        Assert.All(report.Assessments.Where(value => ReferenceCodes.Contains(value.Code)),
            value => Assert.Equal(EquipmentDiagnosticsEvidenceAssessmentStatus.ReferenceOnly, value.Status));
        Assert.Equal(0, report.Summary.ReadyForStagingCandidateCount);
    }

    [Fact]
    public void RuntimeStagingAndConflictGuardsPreventPreview()
    {
        var seed = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries().First();
        var runtime = seed with { SeriesName = "GMV X", Category = EquipmentCategory.VrfOutdoorUnit, Code = "E1" };
        var input = CreateInput(
            [
                Occurrence("E1", "Fault", "TroubleshootingSection", "Outdoor", safety: true),
                Occurrence("F1", "Fault", "TroubleshootingSection", "Outdoor", safety: true),
                Occurrence("E4", "Protection", "TroubleshootingSection", "Outdoor", "Meaning A", true),
                Occurrence("E4", "Protection", "TroubleshootingSection", "Outdoor", "Meaning B", true, "manual-2")
            ],
            [runtime],
            """{"candidates":[{"series":"GMV X","category":"VrfOutdoorUnit","code":"F1"}]}""",
            new Dictionary<string, string>
            {
                ["manual-1"] = "PrimaryTroubleshootingSource",
                ["manual-2"] = "PrimaryTroubleshootingSource"
            });
        var coverage = new EquipmentDiagnosticsCodebookCoverageAnalyzer().Analyze(input);
        var evidence = new EquipmentDiagnosticsEvidenceRuleEngine().Assess(input, coverage);
        var preview = new EquipmentDiagnosticsStagingPreviewGenerator().Generate(evidence);

        Assert.Equal(EquipmentDiagnosticsEvidenceAssessmentStatus.AlreadyRuntimeCovered, evidence.Assessments.Single(value => value.Code == "E1").Status);
        Assert.Equal(EquipmentDiagnosticsEvidenceAssessmentStatus.AlreadyStagingCovered, evidence.Assessments.Single(value => value.Code == "F1").Status);
        Assert.All(evidence.Assessments.Where(value => value.Code == "E4"),
            value => Assert.Equal(EquipmentDiagnosticsEvidenceAssessmentStatus.BlockedByConflict, value.Status));
        Assert.Empty(preview.Candidates);
    }

    [Fact]
    public void RepositoryPreviewCommandWritesArtifactsOnlyAndReportsEvidenceReadyCandidatesHonestly()
    {
        var runtimeBefore = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();
        var stagingFiles = Directory.GetFiles(StagingRoot, "*.json", SearchOption.AllDirectories)
            .ToDictionary(path => path, File.ReadAllText, StringComparer.Ordinal);
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { "run", "--project", ToolProject, "--no-build", "--", "preview-staging-candidates", "--repo-root", TestPaths.RepoRoot })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        process.WaitForExit();

        Assert.Equal(0, process.ExitCode);
        Assert.True(File.Exists(PreviewPath));
        Assert.Equal(stagingFiles, Directory.GetFiles(StagingRoot, "*.json", SearchOption.AllDirectories)
            .ToDictionary(path => path, File.ReadAllText, StringComparer.Ordinal));
        using var preview = JsonDocument.Parse(File.ReadAllText(PreviewPath));
        var readyCodes = preview.RootElement.GetProperty("candidateCodes").EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();
        var runtimeAfter = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

        Assert.Equal(1, preview.RootElement.GetProperty("candidateCount").GetInt32());
        Assert.Equal(["F5"], readyCodes);
        Assert.Equal(JsonSerializer.Serialize(runtimeBefore), JsonSerializer.Serialize(runtimeAfter));
        Assert.DoesNotContain(runtimeAfter, entry => entry.Confidence == DiagnosticConfidence.ManualVerified);
    }

    [Fact]
    public void RepositoryGmv6ServiceOccurrencesKeepExistingStagingCodesOutOfPreview()
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = TestPaths.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (var argument in new[] { "run", "--project", ToolProject, "--no-build", "--", "codebook-coverage", "--repo-root", TestPaths.RepoRoot })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        process.WaitForExit();

        Assert.Equal(0, process.ExitCode);
        using var preview = JsonDocument.Parse(File.ReadAllText(PreviewPath));
        var codes = preview.RootElement.GetProperty("candidateCodes").EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty)
            .ToArray();
        Assert.DoesNotContain("E1", codes);
        Assert.DoesNotContain("E3", codes);
        Assert.DoesNotContain("E4", codes);
        Assert.Equal(["F5"], codes);
        using var coverage = JsonDocument.Parse(File.ReadAllText(CoveragePath));
        Assert.Equal(1, coverage.RootElement.GetProperty("summary").GetProperty("readyForStagingCandidateCount").GetInt32());
        Assert.Equal(0, coverage.RootElement.GetProperty("summary").GetProperty("conflictCount").GetInt32());
        Assert.Empty(coverage.RootElement.GetProperty("conflicts").EnumerateArray());
    }

    private static EquipmentDiagnosticsVerificationInput CreateInput(
        IReadOnlyList<string> occurrences,
        IReadOnlyCollection<AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.EquipmentDiagnosticsKnowledgeEntry>? runtime = null,
        string? staging = null,
        IReadOnlyDictionary<string, string>? usages = null) =>
        new(runtime ?? [], [], staging is null ? [] :
            [new EquipmentDiagnosticsVerificationDocument("staging.json", staging, EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate)],
            [], 0, usages?.Keys.ToHashSet(StringComparer.Ordinal),
            [new EquipmentDiagnosticsVerificationDocument("codebook.json", $$"""{"occurrences":[{{string.Join(",", occurrences)}}]}""",
                EquipmentDiagnosticsVerificationDocumentKind.ManualCodeBook)],
            usages);

    private static string Occurrence(
        string code, string kind, string evidence, string side, string meaning = "Reviewed meaning",
        bool safety = false, string manualId = "manual-1") =>
        $$"""{"manualId":"{{manualId}}","sourceFileName":"manual.pdf","sourceTitle":"Service Manual","page":"PDF 10","section":"Troubleshooting {{code}}","code":"{{code}}","normalizedCode":"{{code.ToUpperInvariant()}}","codeKind":"{{kind}}","equipmentSide":"{{side}}","displayContext":"{{(side == "Indoor" ? "IduDisplay" : side == "Outdoor" ? "OduMainBoardLed" : "TechnicalDocument")}}","series":"GMV X","meaning":"{{meaning}}","requiredMeasurements":["Record supported measurement"],"safetyNotes":{{(safety ? "[\"Qualified technician review is required.\"]" : "[]")}},"limitations":["Preview only; verify exact installed family."],"canBecomeDiagnosticCase":true,"promotionReadiness":"ReferenceOnly","evidenceLevel":"{{evidence}}","shortQuote":null}""";

    private static readonly HashSet<string> ReferenceCodes = ["GUIDE", "TOOL", "A0", "n6", "qA", "db"];
    private static readonly string[] UnsafeFragments = ["bypass", "disable protection", "force run", "short protection", "ignore protection"];
    private static string StagingRoot => Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics", "Knowledge", "staging");
    private static string PreviewPath => Path.Combine(TestPaths.RepoRoot, "artifacts", "verification", "equipment-diagnostics", "staging-candidate-preview.json");
    private static string CoveragePath => Path.Combine(TestPaths.RepoRoot, "artifacts", "verification", "equipment-diagnostics", "codebook-coverage-report.json");
    private static string ToolProject => Path.Combine(TestPaths.RepoRoot, "tools", "AssistantEngineer.Tools.EquipmentDiagnosticsVerification");
}
