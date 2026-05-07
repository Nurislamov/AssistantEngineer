using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1RepositoryCommunicationTests
{
    [Fact]
    public void RepositoryCommunicationFilesExist()
    {
        var requiredFiles = new[]
        {
            RootReadmePath,
            PublicReleaseNotesPath,
            AnnouncementDraftPath,
            TaggingGuidePath,
            CommunicationRunbookPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required repository communication artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void RootReadmeDocumentsEngineeringCoreV1StatusCommandsAndLinks()
    {
        var content = File.ReadAllText(RootReadmePath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1",
            "ClosedV1 as an engineering formula gate",
            ".\\scripts\\engineering-core\\verify-engineering-core-v1.ps1",
            ".\\scripts\\engineering-core\\verify-engineering-core-v1-smoke.ps1",
            ".\\scripts\\engineering-core\\verify-engineering-core-v1-contracts.ps1",
            ".\\scripts\\engineering-core\\assert-engineering-core-v1-release-ready.ps1",
            "docs/engineering-core/README.md",
            "docs/releases/EngineeringCoreV1ReleaseManifest.md",
            "docs/traceability/EngineeringCoreV1TraceabilityMatrix.md"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void RootReadmeKeepsForbiddenReleaseWordingVisibleAsForbidden()
    {
        var content = File.ReadAllText(RootReadmePath);

        Assert.Contains("Forbidden release wording", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EnergyPlus parity achieved", content, StringComparison.Ordinal);
        Assert.Contains("ASHRAE 140 validated", content, StringComparison.Ordinal);
        Assert.Contains("Full ISO 52016 implemented", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicReleaseNotesDocumentIncludedScopeClosedV1MeaningNonClaimsAndFutureValidation()
    {
        var content = File.ReadAllText(PublicReleaseNotesPath);

        var requiredPhrases = new[]
        {
            "Engineering Core V1 is closed as an engineering formula gate",
            "What is included",
            "What ClosedV1 means",
            "What ClosedV1 does not mean",
            "Annual 8760 rule",
            "User-visible transparency",
            "Future validation",
            "exact EnergyPlus numerical parity",
            "ASHRAE 140 validation coverage",
            "latent/moisture/humidity support in V1"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AnnouncementDraftUsesApprovedWordingAndKeepsNonClaimsVisible()
    {
        var content = File.ReadAllText(AnnouncementDraftPath);

        Assert.Contains(
            "Engineering Core V1 is closed as an engineering formula gate",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Recommended wording",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Do not use",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains("EnergyPlus parity achieved", content, StringComparison.Ordinal);
        Assert.Contains("ASHRAE 140 validated", content, StringComparison.Ordinal);
        Assert.Contains("Full ISO 52016 implemented", content, StringComparison.Ordinal);

        Assert.Contains(
            "first real EnergyPlus smoke reference fixture",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TaggingGuideRequiresReleaseReadinessBeforeTaggingAndDocumentsTagCommands()
    {
        var content = File.ReadAllText(TaggingGuidePath);

        Assert.Contains(
            ".\\scripts\\engineering-core\\assert-engineering-core-v1-release-ready.ps1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "git tag -a engineering-core-v1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "git push origin engineering-core-v1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Engineering Core V1 - Closed as engineering formula gate",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "Required non-claims",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CommunicationRunbookDocumentsReadmeReleaseNotesAnnouncementTaggingAndGuardTests()
    {
        var content = File.ReadAllText(CommunicationRunbookPath);

        var requiredPhrases = new[]
        {
            "Repository README",
            "Public release notes",
            "Announcement draft",
            "Tagging guide",
            "EngineeringCoreV1RepositoryCommunicationTests",
            "EnergyPlus parity achieved",
            "ASHRAE 140 validated",
            "Full ISO 52016 implemented",
            "no exact EnergyPlus numerical parity",
            "no ASHRAE 140 validation coverage"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void MainVerificationScriptIncludesRepositoryCommunicationGuardTests()
    {
        var content = File.ReadAllText(MainVerificationScriptPath);

        Assert.Contains(
            "EngineeringCoreV1RepositoryCommunicationTests",
            content,
            StringComparison.Ordinal);
    }

    private static string RootReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "README.md");

    private static string PublicReleaseNotesPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCoreV1PublicReleaseNotes.md");

    private static string AnnouncementDraftPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCoreV1AnnouncementDraft.md");

    private static string TaggingGuidePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "releases", "EngineeringCoreV1TaggingGuide.md");

    private static string CommunicationRunbookPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "runbooks", "EngineeringCoreV1RepositoryCommunicationRunbook.md");

    private static string MainVerificationScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "verify-engineering-core-v1.ps1");
}
