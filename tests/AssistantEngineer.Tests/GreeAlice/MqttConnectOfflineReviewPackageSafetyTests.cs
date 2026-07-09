using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class MqttConnectOfflineReviewPackageSafetyTests
{
    [Fact]
    public void OfflineReviewPacketKeepsLiveActionsBlocked()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-offline-review-packet-summary.md");

        Assert.Contains("CONNECT-only live stage approval: no", text, StringComparison.Ordinal);
        Assert.Contains("Live CONNECT gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("SUBSCRIBE gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("PUBLISH gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Device control gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Runtime integration gate: blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OfflineReviewPacketReferencesOnlyRepositorySafeDocuments()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-offline-review-packet-summary.md");

        Assert.Contains("mqtt-connect-readiness-gate.md", text, StringComparison.Ordinal);
        Assert.Contains("mqtt-connect-human-safety-review-checklist.md", text, StringComparison.Ordinal);
        Assert.Contains("mqtt-connect-safety-review-decision-record.md", text, StringComparison.Ordinal);
        Assert.Contains("mqtt-connect-operator-sign-off-template.md", text, StringComparison.Ordinal);
        Assert.Contains("must not include raw credentials", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FailClosedPolicyDefaultsEveryLiveGateToFalse()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-live-gate-fail-closed-policy.md");

        Assert.Contains("Default live CONNECT permission: false", text, StringComparison.Ordinal);
        Assert.Contains("Default SUBSCRIBE permission: false", text, StringComparison.Ordinal);
        Assert.Contains("Default PUBLISH permission: false", text, StringComparison.Ordinal);
        Assert.Contains("Default device control permission: false", text, StringComparison.Ordinal);
        Assert.Contains("Default runtime integration permission: false", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FailClosedPolicyRequiresKillSwitchBeforeRuntime()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-live-gate-fail-closed-policy.md");

        Assert.Contains("Global bridge disabled by default.", text, StringComparison.Ordinal);
        Assert.Contains("Per-account control disabled by default.", text, StringComparison.Ordinal);
        Assert.Contains("Per-device control disabled by default.", text, StringComparison.Ordinal);
        Assert.Contains("Emergency disable path documented.", text, StringComparison.Ordinal);
        Assert.Contains("No secret values in logs.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FutureLiveProbeBoundaryDoesNotApproveLiveProbe()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-future-live-probe-boundary.md");

        Assert.Contains("It is not that future stage.", text, StringComparison.Ordinal);
        Assert.Contains("Live CONNECT is still blocked.", text, StringComparison.Ordinal);
        Assert.Contains("SUBSCRIBE is still blocked.", text, StringComparison.Ordinal);
        Assert.Contains("PUBLISH is still blocked.", text, StringComparison.Ordinal);
        Assert.Contains("Device control is still blocked.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FutureLiveProbeBoundaryLimitsLaterStageToConnectOnly()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-future-live-probe-boundary.md");

        Assert.Contains("One manually triggered CONNECT-only attempt.", text, StringComparison.Ordinal);
        Assert.Contains("No automatic retry loop.", text, StringComparison.Ordinal);
        Assert.Contains("No background service.", text, StringComparison.Ordinal);
        Assert.Contains("Masked output only.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CombinedDocsPreserveRuntimeIsolation()
    {
        string combined = ReadAllDocs();

        Assert.Contains("No AssistantEngineer.Api integration.", combined, StringComparison.Ordinal);
        Assert.Contains("No Telegram integration.", combined, StringComparison.Ordinal);
        Assert.Contains("No runtime configuration.", combined, StringComparison.Ordinal);
        Assert.Contains("No deployment change.", combined, StringComparison.Ordinal);
        Assert.Contains("No migration.", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void CombinedDocsDoNotMentionForbiddenPublicSourceNames()
    {
        string combined = ReadAllDocs();

        Assert.DoesNotContain("openhab", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-remote", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("st-gree-driver", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("github.com/", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProjectStateRecordsCombinedStageAndNextDecision()
    {
        string text = ReadRepoFile("PROJECT_STATE.md");

        Assert.Contains("GREE-ALICE-26", text, StringComparison.Ordinal);
        Assert.Contains("GREE-ALICE-25", text, StringComparison.Ordinal);
        Assert.Contains("offline review packet", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fail-closed", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GREE-ALICE-30", text, StringComparison.Ordinal);
    }

    private static string ReadAllDocs()
    {
        return string.Join(
            Environment.NewLine,
            ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-offline-review-packet-summary.md"),
            ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-live-gate-fail-closed-policy.md"),
            ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-future-live-probe-boundary.md"),
            ReadRepoFile("docs", "integrations", "gree-alice", "README.md"));
    }

    private static string ReadRepoFile(params string[] relativeParts)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(new[] { root }.Concat(relativeParts).ToArray());

        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);

        return File.ReadAllText(path);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate AssistantEngineer.sln from " + AppContext.BaseDirectory);
    }
}
