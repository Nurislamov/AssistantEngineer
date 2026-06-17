using System.Text.RegularExpressions;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsTelegramReleaseCandidateDocsTests
{
    private static readonly string[] DocumentNames =
    [
        "telegram-closed-beta-release-candidate.md",
        "telegram-closed-beta-operator-limitation-card.md",
        "telegram-closed-beta-smoke-matrix.md"
    ];

    [Fact]
    public void ReleaseCandidateDocumentsExist()
    {
        foreach (var name in DocumentNames)
        {
            Assert.True(File.Exists(DocumentPath(name)), name);
        }
    }

    [Fact]
    public void ReleaseCandidateContainsRequiredSectionsAndEvidencePaths()
    {
        var content = Read("telegram-closed-beta-release-candidate.md");
        AssertContainsAll(content,
            "Release Candidate Scope",
            "Out Of Scope",
            "Required Generated Evidence",
            "Required Verification Before Activation",
            "Manual Review Points",
            "Activation Preconditions",
            "Rollback",
            "Release Decision",
            "release-evidence-summary.md",
            "release-evidence-report.json",
            "telegram-closed-beta-goal-run-report.json");
    }

    [Fact]
    public void OperatorLimitationCardContainsRequiredSafetyBoundary()
    {
        var content = Read("telegram-closed-beta-operator-limitation-card.md");
        AssertContainsAll(content,
            "closed beta only",
            "not a final engineering authority",
            "runtime catalog only",
            "partial",
            "Human verification is required",
            "Do not bypass protections",
            "no electrical or refrigerant hazardous instructions");
    }

    [Fact]
    public void SmokeMatrixContainsEveryRequiredSmokeId()
    {
        var content = Read("telegram-closed-beta-smoke-matrix.md");
        AssertContainsAll(content,
            "SMK-API-HEALTH",
            "SMK-TG-DISABLED",
            "SMK-SECRET-INVALID",
            "SMK-CHAT-ALLOW",
            "SMK-CHAT-DENY",
            "SMK-DISCOVERY-ON",
            "SMK-DISCOVERY-OFF",
            "SMK-START",
            "SMK-HELP",
            "SMK-CODE-KNOWN",
            "SMK-CODE-AMBIGUOUS",
            "SMK-CODE-UNKNOWN",
            "SMK-MESSAGE-LONG",
            "SMK-TEXT-UNSUPPORTED",
            "SMK-OUTBOUND-FAIL",
            "SMK-LOGS-SANITIZED",
            "SMK-ROLLBACK");
    }

    [Fact]
    public void ReleaseCandidateDocumentsContainRequiredLimitationsWithoutForbiddenClaims()
    {
        var combined = string.Join(Environment.NewLine, DocumentNames.Select(Read));
        AssertContainsAll(combined,
            "closed beta only",
            "not production or public release",
            "No real secrets in Git",
            "Telegram is disabled by default",
            "chat ID discovery is disabled by default",
            "Polling disabled by default",
            "database/audit persistence",
            "external monitoring",
            "Runtime catalog is the only final-answer source",
            "are not final diagnosis",
            "Vendor manual coverage is partial");

        foreach (var claim in ForbiddenClaims)
        {
            Assert.DoesNotContain(claim, combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ReleaseCandidateDocumentsContainNoRealCredentialsDomainsChatIdsOrEngineeringCoreCommand()
    {
        var combined = string.Join(Environment.NewLine, DocumentNames.Select(Read));
        Assert.DoesNotMatch(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", combined);
        Assert.DoesNotContain(
            Regex.Matches(combined, @"(?i)\b(?:[a-z0-9-]+\.)+(?:com|net|org|io|dev|cloud)\b"),
            match => !match.Value.EndsWith("example.com", StringComparison.OrdinalIgnoreCase) &&
                     !match.Value.EndsWith("example.test", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotMatch(@"(?i)\bchat\s*id\s*[:=]\s*-?\d+\b", combined);
        Assert.DoesNotMatch(@"(?im)^\s*(?:\.\\)?scripts/engineering-core/verify-engineering-core-v1\.ps1\b", combined);
    }

    private static readonly string[] ForbiddenClaims =
    [
        string.Concat("production ", "ready"),
        string.Concat("public release ", "ready"),
        string.Concat("fully autonomous ", "engineer"),
        string.Concat("autonomous production ", "execution"),
        string.Concat("AI ", "diagnosis"),
        string.Concat("RAG ", "diagnosis"),
        string.Concat("vector search ", "diagnosis"),
        string.Concat("full vendor manual ", "coverage"),
        string.Concat("full ", "parity"),
        string.Concat("ManualVerified ", "promotion")
    ];

    private static void AssertContainsAll(string content, params string[] fragments)
    {
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, content, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string Read(string name) => File.ReadAllText(DocumentPath(name));

    private static string DocumentPath(string name) =>
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", name);
}
