using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticsCodebookCoverageTests
{
    [Fact]
    public void CurrentRepositoryCoverageIsDeterministicAndKeepsReferenceCodesNonPromotable()
    {
        var input = CreateRepositoryInput();
        var analyzer = new EquipmentDiagnosticsCodebookCoverageAnalyzer();

        var first = analyzer.Analyze(input);
        var second = analyzer.Analyze(input);

        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.True(first.Passed);
        Assert.Equal(0, first.Summary.ConflictCount);
        Assert.All(first.Entries.Where(entry => ReferenceCodes.Contains(entry.Code)),
            entry =>
            {
                Assert.Equal(EquipmentDiagnosticsStagingReadinessRecommendation.ReferenceOnly, entry.Readiness);
                Assert.NotEqual(EquipmentDiagnosticsCodeCoverageStatus.ReadyForStagingCandidate, entry.Status);
            });
        Assert.DoesNotContain(first.Entries, entry => entry.Code is "CE41" or "CE42" or "CE52");
    }

    [Fact]
    public void TroubleshootingFaultIsReadyForStagingButStatusIsReferenceOnly()
    {
        var input = CreateSyntheticInput(
            Occurrence("E1", "Fault", "TroubleshootingSection", true, "Outdoor"),
            Occurrence("A0", "Status", "DebuggingProcedure", false, "System"));

        var report = new EquipmentDiagnosticsCodebookCoverageAnalyzer().Analyze(input);

        Assert.Equal(EquipmentDiagnosticsCodeCoverageStatus.ReadyForStagingCandidate,
            report.Entries.Single(entry => entry.Code == "E1").Status);
        Assert.Equal(EquipmentDiagnosticsCodeCoverageStatus.StatusOnly,
            report.Entries.Single(entry => entry.Code == "A0").Status);
        Assert.Equal(1, report.Summary.ReadyForStagingCandidateCount);
    }

    [Fact]
    public void RuntimeAndStagingCoverageTakePrecedenceForMatchingContext()
    {
        var seed = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries().First();
        var runtime = seed with { SeriesName = "GMV X", Category = EquipmentCategory.VrfOutdoorUnit, Code = "E1" };
        var input = CreateSyntheticInput(
            [Occurrence("E1", "Protection", "ErrorIndicationTable", true, "Outdoor"),
             Occurrence("F1", "Fault", "ErrorIndicationTable", true, "Outdoor")],
            [runtime],
            """{"candidates":[{"series":"GMV X","category":"VrfOutdoorUnit","code":"F1"}]}""");

        var report = new EquipmentDiagnosticsCodebookCoverageAnalyzer().Analyze(input);

        Assert.Equal(EquipmentDiagnosticsCodeCoverageStatus.RuntimeCovered, report.Entries.Single(entry => entry.Code == "E1").Status);
        Assert.Equal(EquipmentDiagnosticsCodeCoverageStatus.StagingCovered, report.Entries.Single(entry => entry.Code == "F1").Status);
    }

    [Fact]
    public void SameContextMeaningConflictBlocksWhenRuntimeCoveredButIndoorOutdoorRemainDistinct()
    {
        var seed = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries().First();
        var runtime = seed with { SeriesName = "GMV X", Category = EquipmentCategory.VrfOutdoorUnit, Code = "E1" };
        var outdoorA = Occurrence("E1", "Protection", "ErrorIndicationTable", true, "Outdoor", "Meaning A");
        var outdoorB = Occurrence("E1", "Protection", "ErrorIndicationTable", true, "Outdoor", "Meaning B");
        var indoor = Occurrence("E1", "Fault", "ErrorIndicationTable", true, "Indoor", "Indoor meaning");

        var report = new EquipmentDiagnosticsCodebookCoverageAnalyzer().Analyze(CreateSyntheticInput([outdoorA, outdoorB, indoor], [runtime]));

        Assert.False(report.Passed);
        Assert.Single(report.Conflicts);
        Assert.Equal(EquipmentDiagnosticsVerificationSeverity.Error, report.Conflicts[0].Severity);
        Assert.Equal(2, report.Entries.Count(entry => entry.Code == "E1"));
    }

    [Fact]
    public void VerificationAndPrBodyIncludeCoverageSummary()
    {
        var verification = new EquipmentDiagnosticsVerificationService().Verify(CreateRepositoryInput());
        var branch = new BranchReadinessVerificationService().Verify(new BranchReadinessInput(
            "feature/equipment-diagnostics-codebook-coverage-to-staging", "origin/master", "EquipmentDiagnostics",
            [], verification, []));
        var body = new BranchReadinessPrBodyGenerator().Render(branch, "artifacts/verification/branch-readiness/branch-readiness-report.json");

        Assert.NotNull(verification.CodebookCoverage);
        Assert.Contains("## Manual codebook coverage", body, StringComparison.Ordinal);
        Assert.Contains("Ready for staging candidates", body, StringComparison.Ordinal);
    }

    private static EquipmentDiagnosticsVerificationInput CreateRepositoryInput()
    {
        var root = Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics", "Knowledge");
        return new EquipmentDiagnosticsVerificationInput(
            new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries(), [],
            Directory.GetFiles(Path.Combine(root, "staging"), "*.json", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}examples{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}templates{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Select(path => new EquipmentDiagnosticsVerificationDocument(path, File.ReadAllText(path), EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate)).ToArray(),
            [], 0, null,
            Directory.GetFiles(Path.Combine(root, "manual-codebook"), "*.json", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
                .Select(path => new EquipmentDiagnosticsVerificationDocument(path, File.ReadAllText(path), EquipmentDiagnosticsVerificationDocumentKind.ManualCodeBook)).ToArray());
    }

    private static EquipmentDiagnosticsVerificationInput CreateSyntheticInput(params string[] occurrences) =>
        CreateSyntheticInput(occurrences, []);
    private static EquipmentDiagnosticsVerificationInput CreateSyntheticInput(
        IReadOnlyList<string> occurrences,
        IReadOnlyCollection<AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.EquipmentDiagnosticsKnowledgeEntry> runtime,
        string? staging = null) =>
        new(runtime, [], staging is null ? [] :
            [new EquipmentDiagnosticsVerificationDocument("staging.json", staging, EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate)],
            [], 0, null,
            [new EquipmentDiagnosticsVerificationDocument("codebook.json", $$"""{"occurrences":[{{string.Join(",", occurrences)}}]}""",
                EquipmentDiagnosticsVerificationDocumentKind.ManualCodeBook)]);

    private static string Occurrence(
        string code, string kind, string evidence, bool diagnostic, string side, string meaning = "Reviewed meaning") =>
        $$"""{"manualId":"manual-1","sourceFileName":"manual.pdf","sourceTitle":"Manual","page":"PDF 1","section":"Section","code":"{{code}}","normalizedCode":"{{code.ToUpperInvariant()}}","codeKind":"{{kind}}","equipmentSide":"{{side}}","displayContext":"{{(side == "Indoor" ? "IduDisplay" : "OduMainBoardLed")}}","series":"GMV X","meaning":"{{meaning}}","canBecomeDiagnosticCase":{{diagnostic.ToString().ToLowerInvariant()}},"promotionReadiness":"ReferenceOnly","evidenceLevel":"{{evidence}}"}""";

    private static readonly HashSet<string> ReferenceCodes =
        ["A0", "A3", "A4", "A8", "Ay", "n6", "n7", "n8", "n9", "nb", "nn", "qA", "qH", "qC", "qP", "qU", "db"];
}
