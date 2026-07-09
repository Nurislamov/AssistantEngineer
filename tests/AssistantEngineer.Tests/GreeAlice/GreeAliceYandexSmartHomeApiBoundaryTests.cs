using System;
using System.IO;
using System.Linq;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceYandexSmartHomeApiBoundaryTests
{
    [Fact]
    public void ApiProjectExistsAndIsIncludedInSolution()
    {
        string root = FindRepositoryRoot();
        string projectPath = Path.Combine(
            root,
            "src",
            "Integrations",
            "GreeAliceBridge",
            "AssistantEngineer.GreeAliceBridge.Api",
            "AssistantEngineer.GreeAliceBridge.Api.csproj");
        string solutionText = File.ReadAllText(Path.Combine(root, "AssistantEngineer.sln"));

        Assert.True(File.Exists(projectPath), "Expected isolated GreeAliceBridge API project.");
        Assert.Contains("AssistantEngineer.GreeAliceBridge.Api.csproj", solutionText, StringComparison.Ordinal);
    }

    [Fact]
    public void ApiProjectReferencesOnlyGreeAliceBridgeProjects()
    {
        string text = ReadApiProject();

        Assert.Contains("AssistantEngineer.GreeAliceBridge.Application.csproj", text, StringComparison.Ordinal);
        Assert.Contains("AssistantEngineer.GreeAliceBridge.Contracts.csproj", text, StringComparison.Ordinal);
        Assert.DoesNotContain("src\\Backend\\AssistantEngineer.Api", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiSkeletonDoesNotContainProductionRuntimeWiring()
    {
        string combined = ReadApiSource();

        Assert.DoesNotContain("UseNpgsql", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deploy", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApiSkeletonDoesNotImplementLiveMqttOrDeviceControl()
    {
        string combined = ReadApiSource();

        Assert.DoesNotContain("MqttClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ConnectAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".SubscribeAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PublishAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceControlService", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GreeCloudClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LiveGreeCloud", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAliceBridgeSourceDoesNotContainLiveMqttOrDeviceControlImplementation()
    {
        string combined = ReadBridgeSource();

        Assert.DoesNotContain("MqttClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ConnectAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".SubscribeAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PublishAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceControlService", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GreeRuntimeControlService", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableGreeRuntimeControl", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAliceBridgeSourceDoesNotContainRealCredentialOrSecretMaterial()
    {
        string combined = ReadBridgeSource()
            .Replace("CredentialsStoredOutsideRepository", string.Empty, StringComparison.Ordinal)
            .Replace("RealYandexAppCredentialsAllowed", string.Empty, StringComparison.Ordinal)
            .Replace("RealYandexClientCredentialsConfigured", string.Empty, StringComparison.Ordinal)
            .Replace("RealYandexClientCredentialsAllowedInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("ProductionCredentialsConfigured", string.Empty, StringComparison.Ordinal)
            .Replace("RequiresRealYandexCredentials", string.Empty, StringComparison.Ordinal)
            .Replace("RequiresRealGreeCredentials", string.Empty, StringComparison.Ordinal)
            .Replace("AllowsRealYandexCredentialsInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("no-gree-credentials-in-repo", string.Empty, StringComparison.Ordinal)
            .Replace("credentials-rotation-plan-required", string.Empty, StringComparison.Ordinal)
            .Replace("No real Gree credentials in repository", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Credentials rotation plan required", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("AllowsSecretsInRepository", string.Empty, StringComparison.Ordinal);

        Assert.DoesNotContain("password", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authToken", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("raw-secret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credential", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("macAddress", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAliceBridgeProjectsDoNotReferenceProductionApiTelegramDeploymentOrMigrations()
    {
        string root = FindRepositoryRoot();
        string bridgeRoot = Path.Combine(root, "src", "Integrations", "GreeAliceBridge");
        string combined = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(bridgeRoot, "*.csproj", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("src\\Backend\\AssistantEngineer.Api", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deploy", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadApiProject()
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(
            root,
            "src",
            "Integrations",
            "GreeAliceBridge",
            "AssistantEngineer.GreeAliceBridge.Api",
            "AssistantEngineer.GreeAliceBridge.Api.csproj");

        Assert.True(File.Exists(path), "Expected API project file to exist: " + path);

        return File.ReadAllText(path);
    }

    private static string ReadApiSource()
    {
        string root = FindRepositoryRoot();
        string apiRoot = Path.Combine(
            root,
            "src",
            "Integrations",
            "GreeAliceBridge",
            "AssistantEngineer.GreeAliceBridge.Api");

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));
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
