using System;
using System.IO;
using System.Linq;
using AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceLiveReadOnlyAdapterProposalTests
{
    [Fact]
    public void ProposalDocumentExistsAndBlocksLiveImplementation()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "live-read-only-gree-cloud-adapter-proposal.md");

        Assert.Contains("not a live adapter implementation", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("adds no live Gree+ Cloud calls", text, StringComparison.Ordinal);
        Assert.Contains("Allowed read-only scope", text, StringComparison.Ordinal);
        Assert.Contains("Forbidden scope", text, StringComparison.Ordinal);
        Assert.Contains("MQTT CONNECT", text, StringComparison.Ordinal);
        Assert.Contains("MQTT SUBSCRIBE", text, StringComparison.Ordinal);
        Assert.Contains("MQTT PUBLISH", text, StringComparison.Ordinal);
        Assert.Contains("production runtime wiring", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Credentials must not be stored in the repository", text, StringComparison.Ordinal);
        Assert.Contains("real account identifiers", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must use masked values only", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProposalDocumentsAllowedReadOnlyFieldsAndForbiddenControls()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "live-read-only-gree-cloud-adapter-proposal.md");

        foreach (string phrase in new[]
        {
            "device list / device descriptor",
            "online/offline state",
            "power state",
            "mode",
            "target temperature",
            "current temperature",
            "fan speed",
            "swing state",
            "error/status code"
        })
        {
            Assert.Contains(phrase, text, StringComparison.OrdinalIgnoreCase);
        }

        foreach (string phrase in new[]
        {
            "device control",
            "mode changes",
            "temperature changes",
            "power on/off",
            "fan speed changes",
            "swing changes",
            "scene execution",
            "timers/schedules changes",
            "firmware/update actions",
            "account mutations",
            "device binding/unbinding"
        })
        {
            Assert.Contains(phrase, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ApprovalChecklistDefaultsToNotApprovedAndBlocksLiveRisk()
    {
        string text = ReadRepoFile("docs", "integrations", "gree-alice", "live-read-only-pilot-approval-checklist.md");

        Assert.Contains("Approval status: NOT APPROVED", text, StringComparison.Ordinal);
        Assert.Contains("All tests pass", text, StringComparison.Ordinal);
        Assert.Contains("No secrets in repo", text, StringComparison.Ordinal);
        Assert.Contains("Kill-switch plan documented", text, StringComparison.Ordinal);
        Assert.Contains("Rollback plan documented", text, StringComparison.Ordinal);
        Assert.Contains("Live pilot limited to read-only", text, StringComparison.Ordinal);
        Assert.Contains("Device control remains blocked", text, StringComparison.Ordinal);
        Assert.Contains("MQTT remains blocked", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProposalBoundaryContractsRemainNotApprovedAndDisabled()
    {
        Assert.False(GreeCloudLiveReadOnlyProposalBoundary.LiveReadOnlyPilotApproved);
        Assert.False(GreeCloudLiveReadOnlyProposalBoundary.LiveReadOnlyAdapterImplemented);
        Assert.False(GreeCloudLiveReadOnlyProposalBoundary.LiveReadOnlyAdapterEnabled);
        Assert.False(GreeCloudLiveReadOnlyProposalBoundary.LiveControlAllowed);
        Assert.False(GreeCloudLiveReadOnlyProposalBoundary.MqttAllowed);
        Assert.False(GreeCloudLiveReadOnlyProposalBoundary.ProductionWiringAllowed);
        Assert.Equal("not-approved", GreeCloudLiveReadOnlyProposalBoundary.ApprovalStatus);
    }

    [Fact]
    public void ProposalBoundaryListsAllowedFieldsAndForbiddenOperations()
    {
        Assert.Equal(
            [
                "device_descriptor",
                "online_state",
                "power_state",
                "mode",
                "target_temperature",
                "current_temperature",
                "fan_speed",
                "swing_state",
                "error_status"
            ],
            GreeCloudLiveReadOnlyProposalBoundary.AllowedReadFields);

        foreach (string operation in new[]
        {
            "power_on_off",
            "set_mode",
            "set_temperature",
            "set_fan_speed",
            "set_swing",
            "run_scene",
            "modify_schedule",
            "bind_device",
            "unbind_device",
            "account_mutation",
            "firmware_update",
            "mqtt_connect",
            "mqtt_subscribe",
            "mqtt_publish",
            "production_deploy"
        })
        {
            Assert.Contains(operation, GreeCloudLiveReadOnlyProposalBoundary.ForbiddenOperations);
        }

        Assert.Contains("external_auth_material_outside_repository", GreeCloudLiveReadOnlyProposalBoundary.EvidenceRequirements);
        Assert.Contains("kill_switch_plan", GreeCloudLiveReadOnlyProposalBoundary.EvidenceRequirements);
        Assert.Contains("rollback_plan", GreeCloudLiveReadOnlyProposalBoundary.EvidenceRequirements);
    }

    [Fact]
    public void GreeAliceBridgeSourceContainsNoLiveNetworkOrProductionWiringImplementation()
    {
        string combined = ReadBridgeSource();

        Assert.DoesNotContain("HttpClient", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".GetAsync(", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PostAsync(", combined, StringComparison.OrdinalIgnoreCase);
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
    public void GreeAliceBridgeProjectsRemainIsolatedFromProductionApiTelegramDeploymentsAndMigrations()
    {
        string root = FindRepositoryRoot();
        string bridgeRoot = Path.Combine(root, "src", "Integrations", "GreeAliceBridge");
        string combinedProjects = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(bridgeRoot, "*.csproj", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.DoesNotContain("src\\Backend\\AssistantEngineer.Api", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", combinedProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deploy", combinedProjects, StringComparison.OrdinalIgnoreCase);
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
