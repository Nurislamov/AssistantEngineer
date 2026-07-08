using System;
using System.IO;
using Xunit;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class MqttConnectDryRunContractSafetyTests
{
    [Fact]
    public void DryRunCommand_DefinesExpectedEnvironmentContract()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectDryRunCommand.cs");

        var expectedVariables = new[]
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

        foreach (var variable in expectedVariables)
            Assert.Contains(variable, text, StringComparison.Ordinal);
    }

    [Fact]
    public void DryRunCommand_RejectsTopicPayloadAndControlArguments()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectDryRunCommand.cs");

        var forbiddenArguments = new[]
        {
            "topic",
            "payload",
            "command",
            "cmd",
            "power",
            "pow",
            "setpoint",
            "set-tem",
            "settem",
            "mode",
            "fan",
            "swing"
        };

        Assert.Contains("ForbiddenArgumentNames", text, StringComparison.Ordinal);

        foreach (var argument in forbiddenArguments)
            Assert.Contains($"\"{argument}\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DryRunCommand_MasksProvidedValuesAndStoresNoRawCredentials()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectDryRunCommand.cs");

        Assert.Contains("\"<masked>\"", text, StringComparison.Ordinal);
        Assert.Contains("LengthBucket", text, StringComparison.Ordinal);
        Assert.Contains("OutputContainsRawValues: false", text, StringComparison.Ordinal);
        Assert.Contains("RawCredentialsStored: false", text, StringComparison.Ordinal);
        Assert.Contains("Value: provided ? \"<masked>\" : null", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DryRunCommand_DoesNotContainLiveMqttNetworkImplementation()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/MqttConnectDryRunCommand.cs");

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
                $"MqttConnectDryRunCommand must not contain live MQTT/network implementation marker: {marker}");
        }
    }

    [Fact]
    public void DryRunDocumentation_KeepsAllLiveGatesBlocked()
    {
        var text = ReadRepoFile("docs/integrations/gree-alice/mqtt-connect-dry-run-contract.md");

        Assert.Contains("CONNECT gate: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SUBSCRIBE gate: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PUBLISH gate: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Device control gate: blocked", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Network connection opened: no", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MQTT CONNECT sent: no", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Program_RegistersDryRunCommand()
    {
        var text = ReadRepoFile("tools/AssistantEngineer.Tools.GreeCloudProbe/Program.cs");

        Assert.Contains("--mqtt-connect-dry-run", text, StringComparison.Ordinal);
        Assert.Contains("MqttConnectDryRunCommand.Run(args)", text, StringComparison.Ordinal);
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
