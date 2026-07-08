using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class MqttConnectHumanSafetyReviewChecklistTests
{
    [Fact]
    public void ChecklistKeepsAllLiveMqttAndControlActionsBlocked()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-human-safety-review-checklist.md");

        Assert.Contains("Live CONNECT gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("SUBSCRIBE gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("PUBLISH gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Device control gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("No live MQTT CONNECT.", text, StringComparison.Ordinal);
        Assert.Contains("No MQTT SUBSCRIBE.", text, StringComparison.Ordinal);
        Assert.Contains("No MQTT PUBLISH.", text, StringComparison.Ordinal);
        Assert.Contains("No device control.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChecklistDoesNotApproveLiveConnect()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-human-safety-review-checklist.md");

        Assert.Contains("Human safety review status: not approved", text, StringComparison.Ordinal);
        Assert.Contains("It does not approve live MQTT use.", text, StringComparison.Ordinal);
        Assert.Contains("does not allow live CONNECT by itself", text, StringComparison.Ordinal);
        Assert.Contains("live MQTT remains blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChecklistPreservesIsolationBoundary()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-human-safety-review-checklist.md");

        Assert.Contains("No API integration.", text, StringComparison.Ordinal);
        Assert.Contains("No Telegram integration.", text, StringComparison.Ordinal);
        Assert.Contains("No runtime configuration.", text, StringComparison.Ordinal);
        Assert.Contains("No deployment change.", text, StringComparison.Ordinal);
        Assert.Contains("No migration.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChecklistForbidsSensitiveCommittedArtifacts()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-human-safety-review-checklist.md");

        Assert.Contains("Do not paste raw credentials", text, StringComparison.Ordinal);
        Assert.Contains("No artifact commit.", text, StringComparison.Ordinal);
        Assert.Contains("No raw credential commit.", text, StringComparison.Ordinal);
        Assert.Contains("No token/password/device key/MAC/account identifier commit.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ChecklistDoesNotMentionForbiddenPublicSourceNames()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-human-safety-review-checklist.md");

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
