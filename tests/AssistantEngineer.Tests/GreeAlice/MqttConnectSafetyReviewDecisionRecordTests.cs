using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class MqttConnectSafetyReviewDecisionRecordTests
{
    [Fact]
    public void DecisionRecordKeepsConnectSubscribePublishAndControlBlocked()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-safety-review-decision-record.md");

        Assert.Contains("Decision: blocked-review-incomplete", text, StringComparison.Ordinal);
        Assert.Contains("Ready for live CONNECT: no", text, StringComparison.Ordinal);
        Assert.Contains("Live CONNECT gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("SUBSCRIBE gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("PUBLISH gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Device control gate: blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DecisionRecordRejectsApprovalOutcomes()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-safety-review-decision-record.md");

        Assert.Contains("approved-live-connect", text, StringComparison.Ordinal);
        Assert.Contains("approved-subscribe", text, StringComparison.Ordinal);
        Assert.Contains("approved-publish", text, StringComparison.Ordinal);
        Assert.Contains("approved-device-control", text, StringComparison.Ordinal);
        Assert.Contains("The following outcomes are not approved", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DecisionRecordPreservesRuntimeIsolation()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-safety-review-decision-record.md");

        Assert.Contains("AssistantEngineer.Api integration: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Telegram integration: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Deployment/runtime configuration: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Database migrations: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Cloud bridge runtime integration: blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DecisionRecordDoesNotRequireSensitiveArtifacts()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-safety-review-decision-record.md");

        Assert.Contains("No raw credential", text, StringComparison.Ordinal);
        Assert.Contains("token", text, StringComparison.Ordinal);
        Assert.Contains("password", text, StringComparison.Ordinal);
        Assert.Contains("device key", text, StringComparison.Ordinal);
        Assert.Contains("PCAP", text, StringComparison.Ordinal);
        Assert.Contains("CSV", text, StringComparison.Ordinal);
        Assert.Contains("local artifact", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DecisionRecordDoesNotMentionForbiddenPublicSourceNames()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-safety-review-decision-record.md");

        Assert.DoesNotContain("openhab", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-remote", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("st-gree-driver", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("github.com/", text, StringComparison.OrdinalIgnoreCase);
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
