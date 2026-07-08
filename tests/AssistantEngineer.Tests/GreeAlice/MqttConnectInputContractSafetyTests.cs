using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class MqttConnectInputContractSafetyTests
{
    private static readonly string[] RequiredEnvironmentVariables =
    {
        "GREE_ALICE_MQTT_HOST",
        "GREE_ALICE_MQTT_PORT",
        "GREE_ALICE_MQTT_CLIENT_ID",
        "GREE_ALICE_MQTT_USERNAME",
        "GREE_ALICE_MQTT_PASSWORD",
        "GREE_ALICE_MQTT_TOKEN",
        "GREE_ALICE_MQTT_AUTH_MODE",
        "GREE_ALICE_MQTT_KEEP_ALIVE_SECONDS",
        "GREE_ALICE_MQTT_CONNECT_TIMEOUT_SECONDS",
        "GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK",
        "GREE_ALICE_MQTT_ALLOW_SUBSCRIBE",
        "GREE_ALICE_MQTT_ALLOW_PUBLISH"
    };

    [Fact]
    public void MqttConnectInputValidationCommand_DefinesFailClosedInputContract()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectInputValidationCommand.cs");

        foreach (var variable in RequiredEnvironmentVariables)
            Assert.Contains(variable, text, StringComparison.Ordinal);

        Assert.Contains("blocked-fail-closed", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK", text, StringComparison.Ordinal);
        Assert.Contains("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE", text, StringComparison.Ordinal);
        Assert.Contains("GREE_ALICE_MQTT_ALLOW_PUBLISH", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OfflineValidationAndEvidenceCommands_DoNotContainLiveMqttNetworkImplementation()
    {
        var relativePaths = new[]
        {
            "tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectInputValidationCommand.cs",
            "tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectInputContractCommand.cs",
            "tools/AssistantEngineer.Tools.GreeCloudProbe/MqttEvidenceInventoryCommand.cs",
            "tools/AssistantEngineer.Tools.GreeCloudProbe/MqttEvidenceGateDecisionCommand.cs",
            "tools/AssistantEngineer.Tools.GreeCloudProbe/ControlActionEvidenceCommand.cs"
        };

        var forbiddenImplementationMarkers = new[]
        {
            "new TcpClient",
            "new Socket",
            "new SslStream",
            "SslStream(",
            "MQTTnet",
            "MqttFactory",
            "ManagedMqttClient",
            "ConnectAsync(",
            "PublishAsync(",
            "SubscribeAsync("
        };

        foreach (var relativePath in relativePaths)
        {
            var text = ReadRepoFile(relativePath);

            foreach (var marker in forbiddenImplementationMarkers)
            {
                Assert.False(
                    text.Contains(marker, StringComparison.OrdinalIgnoreCase),
                    $"{relativePath} must not contain live MQTT/network implementation marker: {marker}");
            }
        }
    }

    [Fact]
    public void GreeAliceDocs_DoNotContainThirdPartyProtocolSourceReferences()
    {
        var repoRoot = FindRepoRoot();
        var docsRoot = Path.Combine(repoRoot, "docs", "integrations", "gree-alice");
        var toolReadme = Path.Combine(repoRoot, "tools", "AssistantEngineer.Tools.GreeCloudProbe", "README.md");
        var projectState = Path.Combine(repoRoot, "PROJECT_STATE.md");

        var files = Directory.EnumerateFiles(docsRoot, "*.md", SearchOption.TopDirectoryOnly)
            .Concat(new[] { toolReadme, projectState })
            .Where(File.Exists)
            .ToArray();

        var forbidden = new[]
        {
            "tomikaa87",
            "gree-remote",
            "gree-hvac-mqtt-bridge",
            "HomeAssistant-GreeClimateComponent",
            "RobHofmann",
            "luc10",
            "gree-api-client",
            "github.com/"
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);

            foreach (var term in forbidden)
            {
                Assert.False(
                    text.Contains(term, StringComparison.OrdinalIgnoreCase),
                    $"{file} must not contain third-party protocol source reference: {term}");
            }
        }
    }

    [Fact]
    public void ConnectOnlySafetySpecification_KeepsSubscribePublishAndControlBlocked()
    {
        var text = ReadRepoFile("docs/integrations/gree-alice/mqtt-connect-only-safety-specification.md");

        Assert.Contains("GREE_ALICE_MQTT_DISCONNECT_AFTER_CONNACK=true", text, StringComparison.Ordinal);
        Assert.Contains("GREE_ALICE_MQTT_ALLOW_SUBSCRIBE=false", text, StringComparison.Ordinal);
        Assert.Contains("GREE_ALICE_MQTT_ALLOW_PUBLISH=false", text, StringComparison.Ordinal);
        Assert.Contains("MQTT CONNECT implementation: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MQTT SUBSCRIBE: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MQTT PUBLISH: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Device control: blocked", text, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepoFile(string relativePath)
    {
        var fullPath = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath), $"Expected repository file was not found: {relativePath}");
        return File.ReadAllText(fullPath);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Repository root with AssistantEngineer.sln was not found.");
    }
}
