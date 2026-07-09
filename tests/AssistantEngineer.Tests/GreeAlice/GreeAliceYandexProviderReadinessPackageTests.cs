extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.ProviderReadiness;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceYandexProviderReadinessPackageTests
{
    [Fact]
    public void ProviderReadinessDefaultsAreNotReadyAndNotApproved()
    {
        Assert.Equal("offline-readiness-package", GreeAliceYandexProviderReadinessBoundary.ProviderReadinessMode);
        Assert.Equal("not-ready", GreeAliceYandexProviderReadinessBoundary.ProviderReadinessStatus);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.ProviderRegistrationApproved);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.ProviderPublicationApproved);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.RealYandexProviderCreated);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.RealOAuthImplemented);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.RealOAuthEndpointsImplemented);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.RealYandexClientCredentialsConfigured);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.RealYandexClientCredentialsAllowedInRepository);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.RealTokensIssued);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.TokenStorageImplemented);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.ProductionEndpointConfigured);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.ProductionDeploymentWiringEnabled);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.ManualSmokeRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.SecurityReviewRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.AccountLinkingReviewRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.DeviceContractReviewRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.QueryContractReviewRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.ActionContractReviewRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.UnlinkContractReviewRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.RegistryScopeReviewRequired);
        Assert.True(GreeAliceYandexProviderReadinessBoundary.OperatorApprovalRequired);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.AllowsSecretsInRepository);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.AllowsRealYandexCredentialsInRepository);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.AllowsLiveGreeControl);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.AllowsMqtt);
        Assert.False(GreeAliceYandexProviderReadinessBoundary.AllowsDeviceControl);
    }

    [Fact]
    public void EndpointReadinessListsOfflineSmartHomeAndUnimplementedOAuthGroups()
    {
        GreeAliceYandexProviderReadinessReview review = Evaluate();

        AssertEndpoint(review, "/v1.0/user/devices", "offline-contract-present", implemented: true);
        AssertEndpoint(review, "/v1.0/user/devices/query", "offline-contract-present", implemented: true);
        AssertEndpoint(review, "/v1.0/user/devices/action", "offline-contract-present-fail-closed", implemented: true);
        AssertEndpoint(review, "/v1.0/user/unlink", "offline-contract-present", implemented: true);
        AssertEndpoint(review, "/oauth/authorize", "not-implemented", implemented: false);
        AssertEndpoint(review, "/oauth/token", "not-implemented", implemented: false);
        AssertEndpoint(review, "/oauth/callback", "not-implemented", implemented: false);
        Assert.All(
            review.Endpoints.Where(endpoint => endpoint.EndpointGroup.StartsWith("future-oauth-", StringComparison.Ordinal)),
            endpoint =>
            {
                Assert.False(endpoint.IsImplemented);
                Assert.False(endpoint.IsProductionReady);
                Assert.Equal("not-implemented", endpoint.Status);
            });
    }

    [Fact]
    public void ReadinessEvaluatorReturnsNotReadyOfflinePackage()
    {
        GreeAliceYandexProviderReadinessReview review = Evaluate();

        Assert.Equal("not-ready", review.Status);
        Assert.Equal("offline-readiness-package", review.Mode);
        Assert.NotEmpty(review.Endpoints);
        Assert.NotEmpty(review.Requirements);
        Assert.NotEmpty(review.SecurityChecklist.Items);
        Assert.NotEmpty(review.ManualSmokePlan.Steps);
        Assert.False(review.ProviderRegistrationApproved);
        Assert.False(review.RealOAuthImplemented);
        Assert.False(review.ProductionCredentialsConfigured);
        Assert.False(review.ProductionDeploymentWiringEnabled);
        Assert.False(review.LiveControlEnabled);
        Assert.False(review.ManualSmokePlan.LiveCallsAllowed);
        Assert.Equal("NOT APPROVED", review.SubmissionChecklist.SubmissionStatus);
        Assert.Equal("not-approved", review.OperatorChecklist.OperatorApprovalStatus);
        Assert.Contains(review.Requirements, requirement => !requirement.IsSatisfied);
    }

    [Fact]
    public void RequirementsIncludeContractReviewsAndPendingProductionGates()
    {
        GreeAliceYandexProviderReadinessReview review = Evaluate();

        AssertRequirement(review, "smart-home-devices-contract", satisfied: true);
        AssertRequirement(review, "smart-home-query-contract", satisfied: true);
        AssertRequirement(review, "smart-home-action-contract", satisfied: true);
        AssertRequirement(review, "smart-home-unlink-contract", satisfied: true);
        AssertRequirement(review, "account-linking-review", satisfied: false);
        AssertRequirement(review, "registry-scope-review", satisfied: false);
        AssertRequirement(review, "vrf-child-exposure-review", satisfied: false);
        AssertRequirement(review, "security-checklist-approved", satisfied: false);
        AssertRequirement(review, "manual-smoke-plan-approved", satisfied: false);
        AssertRequirement(review, "production-endpoint-plan-approved", satisfied: false);
        AssertRequirement(review, "live-read-only-pilot-approved-separately", satisfied: false);
        AssertRequirement(review, "live-control-approved-separately", satisfied: false);
        Assert.Equal("not-ready", review.Status);
    }

    [Fact]
    public void SecurityChecklistContainsRequiredSafetyItems()
    {
        GreeAliceYandexProviderSecurityChecklist checklist = Evaluate().SecurityChecklist;

        Assert.Equal("not-approved", checklist.ApprovalStatus);
        AssertChecklist(checklist, "no-secrets-in-repo");
        AssertChecklist(checklist, "no-yandex-client-secret-in-repo");
        AssertChecklist(checklist, "no-access-token-in-repo");
        AssertChecklist(checklist, "no-refresh-token-in-repo");
        AssertChecklist(checklist, "no-gree-credentials-in-repo");
        AssertChecklist(checklist, "no-mac-like-fixtures");
        AssertChecklist(checklist, "masked-evidence-required");
        AssertChecklist(checklist, "unknown-unlinked-users-fail-closed");
        AssertChecklist(checklist, "action-endpoint-fail-closed");
        AssertChecklist(checklist, "mqtt-remains-blocked");
    }

    [Fact]
    public void ManualSmokePlanContainsOnlyOfflineLocalChecks()
    {
        GreeAliceYandexProviderManualSmokePlan plan = Evaluate().ManualSmokePlan;

        Assert.False(plan.LiveCallsAllowed);
        AssertSmokeStep(plan, "build");
        AssertSmokeStep(plan, "tests");
        AssertSmokeStep(plan, "static-safety-scans");
        AssertSmokeStep(plan, "devices-offline-check");
        AssertSmokeStep(plan, "query-offline-check");
        AssertSmokeStep(plan, "action-fail-closed-check");
        AssertSmokeStep(plan, "unlink-offline-check");
        AssertSmokeStep(plan, "account-linking-template-check");
        AssertSmokeStep(plan, "scoped-registry-template-check");
        AssertSmokeStep(plan, "vrf-child-unit-exposure-check");
        AssertSmokeStep(plan, "no-live-call-step");
    }

    [Fact]
    public void ProviderReadinessDocsExistAndKeepNotReadyPosition()
    {
        string readiness = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-provider-readiness-package.md");
        string submission = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-provider-submission-checklist.md");
        string smoke = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-provider-manual-smoke-plan.md");
        string security = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-provider-security-review.md");
        string combined = string.Join(Environment.NewLine, readiness, submission, smoke, security);

        Assert.Contains("Provider readiness is NOT READY by default", readiness, StringComparison.Ordinal);
        Assert.Contains("Real provider registration is NOT APPROVED", readiness, StringComparison.Ordinal);
        Assert.Contains("Real OAuth is not implemented", readiness, StringComparison.Ordinal);
        Assert.Contains("Real Yandex credentials/tokens must not be stored in repo", readiness, StringComparison.Ordinal);
        Assert.Contains("Production endpoint is not configured", readiness, StringComparison.Ordinal);
        Assert.Contains("Production deploy is not enabled", readiness, StringComparison.Ordinal);
        Assert.Contains("Live Gree+ Cloud control is disabled", readiness, StringComparison.Ordinal);
        Assert.Contains("MQTT is blocked", readiness, StringComparison.Ordinal);
        Assert.Contains("Device control remains fail-closed", readiness, StringComparison.Ordinal);
        Assert.Contains("Submission status: NOT APPROVED", submission, StringComparison.Ordinal);
        Assert.Contains("dotnet restore .\\AssistantEngineer.sln", smoke, StringComparison.Ordinal);
        Assert.Contains("Security review status: NOT APPROVED", security, StringComparison.Ordinal);
        AssertNoMacLikeValue(combined);
        Assert.DoesNotContain("clientSecret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExistingYandexBehaviorAndBoundariesRemainOfflineStable()
    {
        Assert.Equal("offline-template", GreeAliceYandexAccountLinkingBoundary.AccountLinkingMode);
        Assert.Equal("offline-template", GreeAliceRegistryImportBoundary.ImportMode);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);

        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexDevicesResponse? devices = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");
        Assert.NotNull(devices);
        Assert.Contains(devices.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-bedroom-001");

        YandexQueryResponse? query = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("dummy-gree-ac-001")]))).Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.Equal("offline-fixture", Assert.Single(query!.Devices).Status);

        YandexActionResponse? action = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    "dummy-gree-ac-001",
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]))).Content.ReadFromJsonAsync<YandexActionResponse>();
        YandexActionDeviceResultDto result = Assert.Single(action!.Devices);
        Assert.Equal("dry-run-fail-closed", result.Status);
        Assert.False(result.SentToGreeCloud);
        Assert.False(result.SentToMqtt);
        Assert.False(result.SentToDevice);

        YandexUnlinkResponse? unlink = await (await client.PostAsync("/v1.0/user/unlink", content: null)).Content.ReadFromJsonAsync<YandexUnlinkResponse>();
        Assert.Equal("offline-no-production-data-touched", unlink!.Status);
    }

    [Fact]
    public void GreeAliceBridgeSourceHasNoLiveOAuthYandexGreeMqttControlOrProductionWiring()
    {
        string source = NormalizeAllowedBoundaryTerms(ReadBridgeSource());
        string projects = ReadBridgeProjects();
        string docsRoot = Path.Combine(FindRepositoryRoot(), "docs", "integrations", "gree-alice");

        Assert.DoesNotContain("MapGet(\"/oauth", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapPost(\"/oauth", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".GetAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PostAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MqttClient", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".ConnectAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".SubscribeAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".PublishAsync(", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeviceControlService", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDevice", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UseProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EnableProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AddProductionRuntimeWiring", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("macAddress", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", projects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", projects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", projects, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(docsRoot, "*.csv", SearchOption.AllDirectories));
    }

    private static GreeAliceYandexProviderReadinessReview Evaluate()
    {
        return new OfflineGreeAliceYandexProviderReadinessEvaluator().Evaluate();
    }

    private static void AssertEndpoint(
        GreeAliceYandexProviderReadinessReview review,
        string path,
        string status,
        bool implemented)
    {
        GreeAliceYandexProviderEndpointReadiness endpoint = Assert.Single(review.Endpoints, item => item.Path == path);

        Assert.Equal(status, endpoint.Status);
        Assert.Equal(implemented, endpoint.IsImplemented);
        Assert.False(endpoint.IsProductionReady);
    }

    private static void AssertRequirement(
        GreeAliceYandexProviderReadinessReview review,
        string requirementId,
        bool satisfied)
    {
        GreeAliceYandexProviderReadinessRequirement requirement = Assert.Single(
            review.Requirements,
            item => item.RequirementId == requirementId);

        Assert.Equal(satisfied, requirement.IsSatisfied);
    }

    private static void AssertChecklist(GreeAliceYandexProviderSecurityChecklist checklist, string requirementId)
    {
        Assert.Contains(checklist.Items, item => item.RequirementId == requirementId);
    }

    private static void AssertSmokeStep(GreeAliceYandexProviderManualSmokePlan plan, string requirementId)
    {
        Assert.Contains(plan.Steps, item => item.RequirementId == requirementId);
    }

    private static void AssertNoMacLikeValue(string value)
    {
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.False(macLike.IsMatch(value), "Value must not look like a hardware identifier: " + value);
    }

    private static string NormalizeAllowedBoundaryTerms(string value)
    {
        return value
            .Replace("RealYandexAppCredentialsAllowed", string.Empty, StringComparison.Ordinal)
            .Replace("RealYandexClientCredentialsConfigured", string.Empty, StringComparison.Ordinal)
            .Replace("RealYandexClientCredentialsAllowedInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("AllowsRealYandexCredentialsInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("RealTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("RefreshTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("AccessTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("TokenStorageImplemented", string.Empty, StringComparison.Ordinal)
            .Replace("TokenRevocationImplemented", string.Empty, StringComparison.Ordinal)
            .Replace("AllowsSecretsInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("DeletedSecrets", string.Empty, StringComparison.Ordinal)
            .Replace("DeletedTokens", string.Empty, StringComparison.Ordinal)
            .Replace("No real Gree credentials", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Credentials rotation plan required", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("OAuth secrets storage plan reviewed", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("No real Yandex client secret in repository", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("No secrets in repository", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("Production secrets must be stored outside repository", string.Empty, StringComparison.OrdinalIgnoreCase);
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

    private static string ReadBridgeProjects()
    {
        string root = FindRepositoryRoot();
        string bridgeRoot = Path.Combine(root, "src", "Integrations", "GreeAliceBridge");

        return string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(bridgeRoot, "*.csproj", SearchOption.AllDirectories)
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
