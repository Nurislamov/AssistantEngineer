using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceOfflineBridgeSkeletonBoundaryTests
{
    [Fact]
    public void PathDecisionChoosesOfflineSkeletonAndKeepsLiveMqttBlocked()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "offline-bridge-path-decision.md");

        Assert.Contains("Chosen path: Path B", text, StringComparison.Ordinal);
        Assert.Contains("offline Yandex Smart Home bridge skeleton first", text, StringComparison.Ordinal);
        Assert.Contains("Live MQTT CONNECT: blocked", text, StringComparison.Ordinal);
        Assert.Contains("MQTT SUBSCRIBE: blocked", text, StringComparison.Ordinal);
        Assert.Contains("MQTT PUBLISH: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Device control: blocked", text, StringComparison.Ordinal);
        Assert.Contains("Gree+ runtime control: blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EndpointContractDefinesYandexSmartHomeEndpointsWithoutImplementation()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-smart-home-offline-endpoint-contract.md");

        Assert.Contains("GET  /v1.0/user/devices", text, StringComparison.Ordinal);
        Assert.Contains("POST /v1.0/user/devices/query", text, StringComparison.Ordinal);
        Assert.Contains("POST /v1.0/user/devices/action", text, StringComparison.Ordinal);
        Assert.Contains("POST /v1.0/user/unlink", text, StringComparison.Ordinal);
        Assert.Contains("It is contract-only.", text, StringComparison.Ordinal);
        Assert.Contains("does not send any request to Gree+ Cloud", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EndpointContractKeepsActionFailClosed()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-smart-home-offline-endpoint-contract.md");

        Assert.Contains("Action sent to Gree+ Cloud: no", text, StringComparison.Ordinal);
        Assert.Contains("Action sent to MQTT: no", text, StringComparison.Ordinal);
        Assert.Contains("Action sent to device: no", text, StringComparison.Ordinal);
        Assert.Contains("Response mode: dry-run fail-closed", text, StringComparison.Ordinal);
    }

    [Fact]
    public void FixtureBoundaryAllowsOnlyDummyData()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "offline-fixture-model-boundary.md");

        Assert.Contains("dummy-gree-ac-001", text, StringComparison.Ordinal);
        Assert.Contains("source: offline-fixture", text, StringComparison.Ordinal);
        Assert.Contains("No real MAC address.", text, StringComparison.Ordinal);
        Assert.Contains("No real Gree+ account id.", text, StringComparison.Ordinal);
        Assert.Contains("No real token.", text, StringComparison.Ordinal);
        Assert.Contains("No real MQTT topic.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CombinedDocsPreserveProjectIsolation()
    {
        string combined = ReadAllDocs();

        Assert.Contains("No production API changes.", combined, StringComparison.Ordinal);
        Assert.Contains("No Telegram changes.", combined, StringComparison.Ordinal);
        Assert.Contains("No runtime deployment changes.", combined, StringComparison.Ordinal);
        Assert.Contains("No migrations.", combined, StringComparison.Ordinal);
        Assert.Contains("No dependency on AssistantEngineer.Api.", combined, StringComparison.Ordinal);
        Assert.Contains("No dependency on Telegram bot runtime.", combined, StringComparison.Ordinal);
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
    public void ProjectStateClosesGreeAlice36AndOpensGreeAlice37()
    {
        string text = ReadRepoFile("PROJECT_STATE.md");

        Assert.Contains("GREE-ALICE-36", text, StringComparison.Ordinal);
        Assert.Contains("CLOSED / pushed", text, StringComparison.Ordinal);
        Assert.Contains("742d4551", text, StringComparison.Ordinal);
        Assert.Contains("Tests: 5506/5506", text, StringComparison.Ordinal);
        Assert.Contains("GREE-ALICE-37", text, StringComparison.Ordinal);
        Assert.Contains("offline bridge skeleton", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadAllDocs()
    {
        return string.Join(
            Environment.NewLine,
            ReadRepoFile("docs", "integrations", "gree-alice", "offline-bridge-path-decision.md"),
            ReadRepoFile("docs", "integrations", "gree-alice", "yandex-smart-home-offline-endpoint-contract.md"),
            ReadRepoFile("docs", "integrations", "gree-alice", "offline-fixture-model-boundary.md"),
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
