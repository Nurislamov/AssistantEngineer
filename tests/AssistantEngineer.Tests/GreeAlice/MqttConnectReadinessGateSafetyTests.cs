using System;
using System.IO;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class MqttConnectReadinessGateSafetyTests
{
    [Fact]
    public void ReadinessGateCommand_DoesNotContainLiveMqttNetworkImplementation()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectReadinessGateCommand.cs");

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

        foreach (var marker in forbiddenImplementationMarkers)
        {
            Assert.False(
                text.Contains(marker, StringComparison.OrdinalIgnoreCase),
                $"MqttConnectReadinessGateCommand must not contain live MQTT/network implementation marker: {marker}");
        }
    }

    [Fact]
    public void ReadinessGateCommand_KeepsLiveConnectBlockedPendingExplicitApproval()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectReadinessGateCommand.cs");

        Assert.Contains("ready-for-human-live-safety-review", text, StringComparison.Ordinal);
        Assert.Contains("blocked-pending-explicit-human-approval", text, StringComparison.Ordinal);
        Assert.Contains("LiveConnectGate", text, StringComparison.Ordinal);
        Assert.Contains("SubscribeGate: \"blocked\"", text, StringComparison.Ordinal);
        Assert.Contains("PublishGate: \"blocked\"", text, StringComparison.Ordinal);
        Assert.Contains("DeviceControlGate: \"blocked\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessGateCommand_RequiresSafeDryRunEvidence()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectReadinessGateCommand.cs");

        Assert.Contains("dry-run-ready-for-separate-live-safety-stage", text, StringComparison.Ordinal);
        Assert.Contains("Dry-run CONNECT gate must remain blocked.", text, StringComparison.Ordinal);
        Assert.Contains("Dry-run SUBSCRIBE gate must remain blocked.", text, StringComparison.Ordinal);
        Assert.Contains("Dry-run PUBLISH gate must remain blocked.", text, StringComparison.Ordinal);
        Assert.Contains("Dry-run device-control gate must remain blocked.", text, StringComparison.Ordinal);
        Assert.Contains("Dry-run report says output contains raw values.", text, StringComparison.Ordinal);
        Assert.Contains("Dry-run report says a network connection was opened.", text, StringComparison.Ordinal);
        Assert.Contains("Dry-run report says MQTT CONNECT was sent.", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadinessGateDocumentation_KeepsLiveMqttBlocked()
    {
        var text = ReadRepoFile("docs/integrations/gree-alice/mqtt-connect-readiness-gate.md");

        Assert.Contains("Live CONNECT gate: blocked-pending-explicit-human-approval", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SUBSCRIBE gate: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PUBLISH gate: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Device control gate: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Network connection opened: no", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MQTT CONNECT sent: no", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Program_RegistersReadinessGateCommand()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/Program.cs");

        Assert.Contains("--mqtt-connect-readiness-gate", text, StringComparison.Ordinal);
        Assert.Contains("MqttConnectReadinessGateCommand.Run(args)", text, StringComparison.Ordinal);
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
