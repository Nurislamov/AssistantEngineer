using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class MqttConnectOperatorSignOffTemplateTests
{
    [Fact]
    public void SignOffTemplateKeepsAllLiveActionsBlocked()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-operator-sign-off-template.md");

        Assert.Contains("Operator sign-off status: not signed", text, StringComparison.Ordinal);
        Assert.Contains("Live CONNECT gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("SUBSCRIBE gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("PUBLISH gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Device control gate: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Runtime integration gate: blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SignOffTemplateDoesNotApproveLiveMqtt()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-operator-sign-off-template.md");

        Assert.Contains("This template does not approve live MQTT CONNECT.", text, StringComparison.Ordinal);
        Assert.Contains("This template does not approve MQTT SUBSCRIBE.", text, StringComparison.Ordinal);
        Assert.Contains("This template does not approve MQTT PUBLISH.", text, StringComparison.Ordinal);
        Assert.Contains("This template does not approve device control.", text, StringComparison.Ordinal);
        Assert.Contains("still does not permit live CONNECT", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SignOffTemplateForbidsSensitiveCommittedData()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-operator-sign-off-template.md");

        Assert.Contains("Do not write any of the following", text, StringComparison.Ordinal);
        Assert.Contains("raw credentials", text, StringComparison.Ordinal);
        Assert.Contains("token values", text, StringComparison.Ordinal);
        Assert.Contains("password values", text, StringComparison.Ordinal);
        Assert.Contains("device key values", text, StringComparison.Ordinal);
        Assert.Contains("MAC addresses", text, StringComparison.Ordinal);
        Assert.Contains("account identifiers", text, StringComparison.Ordinal);
        Assert.Contains("PCAP files or packet payloads", text, StringComparison.Ordinal);
        Assert.Contains("CSV exports", text, StringComparison.Ordinal);
        Assert.Contains("artifacts/ files", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SignOffTemplatePreservesProjectIsolation()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-operator-sign-off-template.md");

        Assert.Contains("AssistantEngineer.Api integration remains blocked", text, StringComparison.Ordinal);
        Assert.Contains("Telegram integration remains blocked", text, StringComparison.Ordinal);
        Assert.Contains("runtime config changes remain blocked", text, StringComparison.Ordinal);
        Assert.Contains("deployment changes remain blocked", text, StringComparison.Ordinal);
        Assert.Contains("migrations remain blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SignOffTemplateUsesAllowedResultValues()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-operator-sign-off-template.md");

        Assert.Contains("blocked-review-incomplete", text, StringComparison.Ordinal);
        Assert.Contains("blocked-safety-risk", text, StringComparison.Ordinal);
        Assert.Contains("ready-for-separate-connect-only-safety-stage", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SignOffTemplateDoesNotMentionForbiddenPublicSourceNames()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "mqtt-connect-operator-sign-off-template.md");

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
