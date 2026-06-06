using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Verification;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticsGmv6ManualBackedStagingTests
{
    private static readonly string[] ExpectedCodes = ["E1", "E3", "E4", "F0", "F1", "F3", "C5", "C7"];
    private static readonly string[] UnsafeFragments =
        ["bypass", "disable protection", "disable protections", "force run", "short protection", "ignore protection"];

    [Fact]
    public void ManualSourceRegistryIsValidUniqueAndNonRuntime()
    {
        using var registry = ReadJson(RegistryPath);
        var sources = registry.RootElement.GetProperty("manualSources").EnumerateArray().ToArray();
        var ids = sources.Select(source => RequiredText(source, "manualId")).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.All(sources, source =>
        {
            Assert.Equal("Gree", RequiredText(source, "manufacturer"));
            Assert.False(string.IsNullOrWhiteSpace(RequiredText(source, "title")));
            Assert.Contains(
                RequiredText(source, "usage"),
                new[] { "PrimaryTroubleshootingSource", "SecondarySafetySource", "IndoorTroubleshootingSource",
                    "ControllerOperationSource", "CommissioningToolSource", "OwnerReferenceSource",
                    "TechnicalApplicabilitySource" });
            Assert.Contains("must not be committed", RequiredText(source, "commitPolicy"), StringComparison.OrdinalIgnoreCase);
        });
        Assert.DoesNotContain(
            new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries(),
            entry => string.Equals(entry.SeriesName, "GMV6", StringComparison.Ordinal));
        Assert.DoesNotContain(GetRuntimeCatalogFiles(), path =>
            path.Contains("manual-sources", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RegistryClassifiesPrimarySecondaryAndIndoorSourcesHonestly()
    {
        using var registry = ReadJson(RegistryPath);
        var sources = registry.RootElement.GetProperty("manualSources").EnumerateArray().ToArray();

        var primary = FindSource(sources, "gree-gmv6-service-manual-gc202001-i");
        Assert.Equal("GC202001-I", RequiredText(primary, "documentCode"));
        Assert.Equal("PrimaryTroubleshootingSource", RequiredText(primary, "usage"));
        Assert.Equal(305, primary.GetProperty("pageCount").GetInt32());

        var owner = FindSource(sources, "gree-gmv6-owners-manual");
        Assert.Equal("SecondarySafetySource", RequiredText(owner, "usage"));

        var indoor = FindSource(sources, "gree-gmv5-indoor-service-manual-gc201711-v");
        Assert.Equal("IndoorTroubleshootingSource", RequiredText(indoor, "usage"));
        Assert.Equal("GC201711-V", RequiredText(indoor, "documentCode"));
    }

    [Fact]
    public void Gmv6OutdoorCandidatesValidateAndRemainReadyForReview()
    {
        var result = new EquipmentDiagnosticsStagingValidator().ValidateJson(
            File.ReadAllText(CandidatesPath),
            new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries(),
            "gmv6-outdoor-manual-backed-candidates.json");
        using var candidates = ReadJson(CandidatesPath);
        var entries = candidates.RootElement.GetProperty("candidates").EnumerateArray().ToArray();

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Report);
        Assert.False(result.Report.PromotionReady);
        Assert.Equal(ExpectedCodes.OrderBy(code => code), entries.Select(entry => RequiredText(entry, "code")).OrderBy(code => code));
        Assert.All(entries, entry =>
        {
            Assert.Equal("GMV6", RequiredText(entry, "series"));
            Assert.Equal("VrfOutdoorUnit", RequiredText(entry, "category"));
            Assert.Equal("ReadyForReview", RequiredText(entry, "reviewStatus"));
            Assert.NotEqual("ApprovedForCatalog", RequiredText(entry, "reviewStatus"));
            Assert.NotEqual(DiagnosticConfidence.ManualVerified.ToString(), RequiredText(entry, "proposedConfidence"));
        });
    }

    [Fact]
    public void ManualPageVerifiedCandidatesReferenceKnownPrimaryManualAndExactAnchors()
    {
        using var registry = ReadJson(RegistryPath);
        var sourceIds = registry.RootElement.GetProperty("manualSources")
            .EnumerateArray()
            .Select(source => RequiredText(source, "manualId"))
            .ToHashSet(StringComparer.Ordinal);
        using var candidates = ReadJson(CandidatesPath);

        foreach (var candidate in candidates.RootElement.GetProperty("candidates").EnumerateArray())
        {
            var code = RequiredText(candidate, "code");
            var source = candidate.GetProperty("source");
            var manualId = RequiredText(source, "manualId");

            Assert.Contains(manualId, sourceIds);
            Assert.Equal("gree-gmv6-service-manual-gc202001-i", manualId);
            Assert.Equal("ServiceManual", RequiredText(source, "sourceType"));
            Assert.Equal("ManualPageVerified", RequiredText(source, "evidenceLevel"));
            Assert.Equal("GC202001-I", RequiredText(source, "manualDocumentCode"));
            Assert.StartsWith("PDF ", RequiredText(source, "page"), StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(RequiredText(source, "section")));
            Assert.Equal(JsonValueKind.Null, source.GetProperty("quote").ValueKind);

            if (code == "C7")
            {
                Assert.Contains("table evidence", RequiredText(source, "notes"), StringComparison.OrdinalIgnoreCase);
                Assert.Contains(source.GetProperty("limitations").EnumerateArray(), limitation =>
                    limitation.GetString()!.Contains("Detailed troubleshooting section still requires review.", StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void IndoorManualIsNotUsedByGmv6OutdoorCandidates()
    {
        using var candidates = ReadJson(CandidatesPath);

        Assert.DoesNotContain(
            candidates.RootElement.GetProperty("candidates").EnumerateArray(),
            candidate => RequiredText(candidate.GetProperty("source"), "manualId") ==
                "gree-gmv5-indoor-service-manual-gc201711-v");
    }

    [Fact]
    public void PdfFilesAreIgnoredAndNotEmbeddedResources()
    {
        var project = File.ReadAllText(ModuleProjectPath);

        Assert.DoesNotContain(".pdf", project, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("manual-intake/sources", project, StringComparison.OrdinalIgnoreCase);
        Assert.All(Directory.GetFiles(LocalManualSourceRoot, "*.pdf"), path =>
            Assert.False(File.Exists(Path.Combine(TestPaths.RepoRoot, Path.GetRelativePath(LocalManualSourceRoot, path)))));
    }

    [Fact]
    public void ManualRegistryAndCandidatesContainNoUnsafeWording()
    {
        var content = File.ReadAllText(RegistryPath) + File.ReadAllText(CandidatesPath);

        Assert.All(UnsafeFragments, fragment =>
            Assert.DoesNotContain(fragment, content, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VerificationReportBlocksUnknownManualId()
    {
        var json = File.ReadAllText(CandidatesPath)
            .Replace("gree-gmv6-service-manual-gc202001-i", "unknown-manual-id", StringComparison.Ordinal);
        var input = new EquipmentDiagnosticsVerificationInput(
            RuntimeEntries: new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries(),
            RuntimeDocuments: [],
            StagingDocuments:
            [
                new EquipmentDiagnosticsVerificationDocument(
                    "synthetic-unknown-manual.json",
                    json,
                    EquipmentDiagnosticsVerificationDocumentKind.StagingCandidate)
            ],
            DocsExampleDocuments: [],
            MinimumRuntimeCatalogCount: 0,
            KnownManualIds: new HashSet<string>(StringComparer.Ordinal) { "gree-gmv6-service-manual-gc202001-i" });

        var report = new EquipmentDiagnosticsVerificationService().Verify(input);

        Assert.False(report.IsReleaseReady);
        Assert.Contains(report.Sections.Single(section => section.Name == "staging-candidates").Issues, issue =>
            issue.Code == "UnknownManualId");
    }

    private static JsonDocument ReadJson(string path) => JsonDocument.Parse(File.ReadAllText(path));

    private static JsonElement FindSource(IEnumerable<JsonElement> sources, string manualId) =>
        sources.Single(source => RequiredText(source, "manualId") == manualId);

    private static string RequiredText(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString() ??
        throw new InvalidOperationException($"Missing text property '{propertyName}'.");

    private static IReadOnlyList<string> GetRuntimeCatalogFiles() =>
        Directory.GetFiles(KnowledgeRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}staging{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}manual-codebook{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static string ModuleRoot =>
        Path.Combine(TestPaths.RepoRoot, "src", "Backend", "AssistantEngineer.Modules.EquipmentDiagnostics");

    private static string KnowledgeRoot => Path.Combine(ModuleRoot, "Knowledge");

    private static string RegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "manual-sources", "gree-manual-sources.json");

    private static string CandidatesPath =>
        Path.Combine(KnowledgeRoot, "staging", "gree", "gmv6-outdoor-manual-backed-candidates.json");

    private static string ModuleProjectPath =>
        Path.Combine(ModuleRoot, "AssistantEngineer.Modules.EquipmentDiagnostics.csproj");

    private static string LocalManualSourceRoot =>
        Path.Combine(TestPaths.RepoRoot, "artifacts", "manual-intake", "sources", "gree");
}
