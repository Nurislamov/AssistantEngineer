using System;
using System.IO;
using System.Linq;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceIsolatedStagingDeploySkeletonTests
{
    [Fact]
    public void IsolatedStagingDeploySkeletonDocumentExistsAndKeepsOfflineBoundary()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "isolated-staging-deploy-skeleton.md");

        Assert.Contains("isolated", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("offline-only", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fail-closed", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AssistantEngineer.GreeAliceBridge.Api", text, StringComparison.Ordinal);
        Assert.Contains("production `AssistantEngineer.Api` is not modified", text, StringComparison.Ordinal);
        Assert.Contains("Telegram runtime is not modified", text, StringComparison.Ordinal);
        Assert.Contains("production deployment scripts are not modified", text, StringComparison.Ordinal);
    }

    [Fact]
    public void IsolatedStagingDeploySkeletonDocumentsLiveOperationsAsDisabled()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "isolated-staging-deploy-skeleton.md");

        Assert.Contains("Live Gree+ Cloud calls: disabled", text, StringComparison.Ordinal);
        Assert.Contains("MQTT CONNECT: disabled", text, StringComparison.Ordinal);
        Assert.Contains("MQTT SUBSCRIBE: disabled", text, StringComparison.Ordinal);
        Assert.Contains("MQTT PUBLISH: disabled", text, StringComparison.Ordinal);
        Assert.Contains("Device control: disabled", text, StringComparison.Ordinal);
        Assert.Contains("Production runtime wiring: disabled", text, StringComparison.Ordinal);
        Assert.Contains("Deployment changes: none", text, StringComparison.Ordinal);
        Assert.Contains("Migrations: none", text, StringComparison.Ordinal);
    }

    [Fact]
    public void IsolatedStagingLocalRunGuideDoesNotRequireCredentialsOrProductionConfig()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "isolated-staging-deploy-skeleton.md");

        Assert.Contains("dotnet run --project .\\src\\Integrations\\GreeAliceBridge\\AssistantEngineer.GreeAliceBridge.Api\\AssistantEngineer.GreeAliceBridge.Api.csproj", text, StringComparison.Ordinal);
        Assert.Contains("must not require", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Gree+ credentials", text, StringComparison.Ordinal);
        Assert.Contains("MQTT broker settings", text, StringComparison.Ordinal);
        Assert.Contains("production connection strings", text, StringComparison.Ordinal);
        Assert.Contains("production deployment scripts", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GreeAliceBridgeApiProjectRemainsIsolatedFromProductionApiAndTelegram()
    {
        string projectText = ReadRepoFile(
            "src",
            "Integrations",
            "GreeAliceBridge",
            "AssistantEngineer.GreeAliceBridge.Api",
            "AssistantEngineer.GreeAliceBridge.Api.csproj");

        Assert.DoesNotContain("src\\Backend\\AssistantEngineer.Api", projectText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", projectText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", projectText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", projectText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deploy", projectText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAliceBridgeSourceContainsNoProductionDeploymentOrLiveControlWiring()
    {
        string combined = ReadBridgeSource();

        Assert.DoesNotContain("MqttClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ConnectAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".SubscribeAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PublishAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceControlService", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UseProductionRuntimeWiring", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableProductionRuntimeWiring", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AddProductionRuntimeWiring", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAlicePublicDocsDoNotMentionForbiddenSourceNames()
    {
        string root = FindRepositoryRoot();
        string docsRoot = Path.Combine(root, "docs", "integrations", "gree-alice");
        string combined = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.TopDirectoryOnly)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("openhab", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gree-remote", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("st-gree-driver", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("github.com/", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadBridgeSource()
    {
        string root = FindRepositoryRoot();
        string bridgeRoot = Path.Combine(root, "src", "Integrations", "GreeAliceBridge");

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(bridgeRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
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
