extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Contracts;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceYandexAccountLinkingBoundaryTests
{
    [Fact]
    public void AccountLinkingBoundaryDefaultsAreOfflineTemplateAndNotApproved()
    {
        Assert.Equal("offline-template", GreeAliceYandexAccountLinkingBoundary.AccountLinkingMode);
        Assert.Equal("not-approved", GreeAliceYandexAccountLinkingBoundary.AccountLinkingStatus);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.RealOAuthImplemented);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.RealYandexProviderRegistrationImplemented);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.RealYandexAppCredentialsAllowed);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.RealTokensIssued);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.RefreshTokensIssued);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.AccessTokensIssued);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.TokenStorageImplemented);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.TokenRevocationImplemented);
        Assert.True(GreeAliceYandexAccountLinkingBoundary.RequiresManualReview);
        Assert.True(GreeAliceYandexAccountLinkingBoundary.RequiresRegistryScopeBinding);
        Assert.True(GreeAliceYandexAccountLinkingBoundary.RequiresMaskedYandexUserId);
        Assert.True(GreeAliceYandexAccountLinkingBoundary.RequiresDummyOrTemplateData);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.AllowsSecretsInRepository);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.AllowsRealYandexUserIdentifiersInRepository);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.AllowsRealBridgeAccountIdentifiersInRepository);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.AllowsProductionWrite);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.AllowsMqtt);
        Assert.False(GreeAliceYandexAccountLinkingBoundary.ProductionWiringAllowed);
    }

    [Fact]
    public void TemplateProviderReturnsDummyMaskedLinkingFixture()
    {
        GreeAliceYandexAccountLinkingTemplate template = CreateTemplate();
        string combined = CombineTemplateText(template);

        Assert.Equal("dummy-link-session-001", template.Session.LinkingSessionId);
        Assert.Equal("offline-template", template.Session.Mode);
        Assert.Equal("not-approved", template.Session.Status);
        Assert.Equal("masked-yandex-user-001", template.Session.YandexUserReference);
        Assert.Equal("dummy-bridge-account-001", template.Session.BridgeAccountReference);
        Assert.Equal("dummy-registry-scope-001", template.Session.RegistryScopeReference);
        Assert.True(template.Session.IsMasked);
        Assert.True(template.Session.IsDummyOrTemplate);
        Assert.True(template.ActiveBinding.IsActive);
        Assert.True(template.ActiveBinding.IsMasked);
        Assert.True(template.ActiveBinding.IsDummyOrTemplate);
        Assert.Contains("dummy-gree-ac-001", template.RegistryScope.AllowedDeviceIds);
        Assert.Contains("dummy-vrf-child-living-001", template.RegistryScope.AllowedVrfChildUnitIds);
        Assert.Contains("dummy-vrf-child-bedroom-001", template.RegistryScope.AllowedVrfChildUnitIds);
        Assert.True(template.UnlinkRequest.IsDummyOrTemplate);
        Assert.True(template.UnlinkResult.IsDummyOrTemplate);
        AssertNoMacLikeOrRealSecretMaterial(combined);
    }

    [Fact]
    public void ValidatorAcceptsValidTemplateSessionBindingAndScope()
    {
        GreeAliceYandexAccountLinkingTemplate template = CreateTemplate();
        OfflineGreeAliceYandexAccountLinkingValidator validator = new();

        Assert.True(validator.ValidateTemplate(template).IsAccepted);
        Assert.True(validator.ValidateSession(template.Session).IsAccepted);
        Assert.True(validator.ValidateBinding(template.ActiveBinding).IsAccepted);
        Assert.True(validator.ValidateScope(template.RegistryScope).IsAccepted);
        Assert.Contains("dummy-gree-ac-001", template.RegistryScope.AllowedDeviceIds);
        Assert.Contains("dummy-vrf-child-living-001", template.RegistryScope.AllowedVrfChildUnitIds);
        Assert.Contains("dummy-vrf-child-bedroom-001", template.RegistryScope.AllowedVrfChildUnitIds);
        Assert.DoesNotContain("*", template.RegistryScope.AllowedDeviceIds);
        Assert.DoesNotContain("all", template.RegistryScope.AllowedDeviceIds);
        Assert.DoesNotContain("global", template.RegistryScope.AllowedDeviceIds);
    }

    [Theory]
    [InlineData("yandexUser.unmasked")]
    [InlineData("scope.missing")]
    [InlineData("scope.globalWildcard")]
    [InlineData("value.sensitive")]
    [InlineData("value.macLike")]
    [InlineData("value.realLike")]
    [InlineData("binding.inactive")]
    [InlineData("binding.unknown")]
    [InlineData("reference.notTemplate")]
    public void ValidatorReturnsControlledIssuesForUnsafeLinkingInputs(string expectedCode)
    {
        OfflineGreeAliceYandexAccountLinkingValidator validator = new();
        GreeAliceYandexAccountLinkingTemplate template = CreateTemplate();

        GreeAliceYandexAccountLinkingValidationResult result = expectedCode switch
        {
            "yandexUser.unmasked" => validator.ValidateBinding(template.ActiveBinding with { YandexUserReference = "dummy-yandex-user-001" }),
            "scope.missing" => validator.ValidateScope(null),
            "scope.globalWildcard" => validator.ValidateScope(template.RegistryScope with { AllowedDeviceIds = ["*"] }),
            "value.sensitive" => validator.ValidateSession(template.Session with { LinkingSessionId = "dummy-clientSecret-marker-001" }),
            "value.macLike" => validator.ValidateScope(template.RegistryScope with { AllowedDeviceIds = ["dummy-aa:bb:cc:dd:ee:ff"] }),
            "value.realLike" => validator.ValidateBinding(template.ActiveBinding with { YandexUserReference = "real-yandex-user-001" }),
            "binding.inactive" => validator.ValidateBinding(template.ActiveBinding with { IsActive = false }),
            "binding.unknown" => validator.ValidateBinding(null),
            "reference.notTemplate" => validator.ValidateBinding(template.ActiveBinding with { BridgeAccountReference = "bridge-account-001" }),
            _ => throw new InvalidOperationException("Unexpected validation case: " + expectedCode)
        };

        Assert.False(result.IsAccepted);
        Assert.Contains(result.Issues, issue => issue.Code == expectedCode);

        if (expectedCode is "binding.inactive" or "binding.unknown")
        {
            Assert.True(result.IsFailClosed);
        }
    }

    [Theory]
    [InlineData("dummy-access-token-marker-001")]
    [InlineData("dummy-refresh-token-marker-001")]
    [InlineData("dummy-authcode-marker-001")]
    [InlineData("real-bridge-account-001")]
    public void ValidatorRejectsTokenCodeAndRealBridgeAccountLookingValues(string unsafeValue)
    {
        OfflineGreeAliceYandexAccountLinkingValidator validator = new();
        GreeAliceYandexAccountLinkingTemplate template = CreateTemplate();

        GreeAliceYandexAccountLinkingValidationResult result = validator.ValidateBinding(
            template.ActiveBinding with { BridgeAccountReference = unsafeValue });

        Assert.False(result.IsAccepted);
        Assert.NotEmpty(result.Issues);
    }

    [Fact]
    public void ScopedRegistryResolverReturnsOnlyExplicitTemplateScope()
    {
        GreeAliceYandexAccountLinkingTemplate template = CreateTemplate();
        OfflineGreeAliceYandexScopedRegistryResolver resolver = new();

        GreeAliceRegistryScopeBinding known = resolver.Resolve("masked-yandex-user-001");
        GreeAliceRegistryScopeBinding unknown = resolver.Resolve("masked-yandex-user-unknown");
        GreeAliceRegistryScopeBinding inactive = resolver.Resolve(template.ActiveBinding with { IsActive = false });
        GreeAliceRegistryScopeBinding unlinked = resolver.Resolve(template.ActiveBinding with { UnlinkedAtUtc = DateTimeOffset.UtcNow });

        Assert.Equal("active-template", known.Status);
        Assert.Contains("dummy-gree-ac-001", known.AllowedDeviceIds);
        Assert.Contains("dummy-vrf-child-living-001", known.AllowedDeviceIds);
        Assert.Contains("dummy-vrf-child-bedroom-001", known.AllowedDeviceIds);
        Assert.DoesNotContain("dummy-vrf-gateway-001", known.AllowedDeviceIds);
        Assert.Equal("fail-closed", unknown.Status);
        Assert.Empty(unknown.AllowedDeviceIds);
        Assert.Equal("fail-closed", inactive.Status);
        Assert.Empty(inactive.AllowedDeviceIds);
        Assert.Equal("fail-closed", unlinked.Status);
        Assert.Empty(unlinked.AllowedDeviceIds);
    }

    [Fact]
    public void TemplateUnlinkRevokesScopeAccessWithoutClaimingRealTokenOrSecretDeletion()
    {
        GreeAliceYandexAccountUnlinkResult result = CreateTemplate().UnlinkResult;

        Assert.Equal("masked-yandex-user-001", result.YandexUserReference);
        Assert.True(result.WasLinked);
        Assert.True(result.IsNowUnlinked);
        Assert.True(result.RevokedAccessToRegistryScope);
        Assert.False(result.DeletedSecrets);
        Assert.False(result.DeletedTokens);
        Assert.False(result.RealTokenStorageImplemented);
        Assert.Equal("offline-template-unlink", result.Reason);
    }

    [Fact]
    public async Task ExistingYandexEndpointsRemainOfflineStableAndFailClosedForActions()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = new();
        using HttpClient client = factory.CreateClient();

        YandexDevicesResponse? devices = await client.GetFromJsonAsync<YandexDevicesResponse>("/v1.0/user/devices");
        Assert.NotNull(devices);
        Assert.Contains(devices.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-living-001");
        Assert.Contains(devices.Devices, device => device.Id == "yandex-dummy-vrf-child-bedroom-001");

        HttpResponseMessage queryResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([new YandexDeviceRequestDto("dummy-gree-ac-001")]));
        YandexQueryResponse? queryBody = await queryResponse.Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.Equal("offline-fixture", Assert.Single(queryBody!.Devices).Status);

        HttpResponseMessage actionResponse = await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest(
                [new YandexActionDeviceRequestDto(
                    "dummy-gree-ac-001",
                    [new YandexActionCapabilityRequestDto("devices.capabilities.on_off", "set", "true")])]));
        YandexActionResponse? actionBody = await actionResponse.Content.ReadFromJsonAsync<YandexActionResponse>();
        YandexActionDeviceResultDto action = Assert.Single(actionBody!.Devices);
        Assert.Equal("dry-run-fail-closed", action.Status);
        Assert.False(action.SentToGreeCloud);
        Assert.False(action.SentToMqtt);
        Assert.False(action.SentToDevice);

        YandexUnlinkResponse? unlink = await (await client.PostAsync("/v1.0/user/unlink", content: null)).Content.ReadFromJsonAsync<YandexUnlinkResponse>();
        Assert.NotNull(unlink);
        Assert.Equal("offline-no-production-data-touched", unlink.Status);
    }

    [Fact]
    public void ExistingRegistryImportAndProductionBoundariesRemainBlocked()
    {
        Assert.Equal("offline-template", GreeAliceRegistryImportBoundary.ImportMode);
        Assert.False(GreeAliceRegistryImportBoundary.RealImportEnabled);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);
        Assert.False(GreeAliceYandexAccountLinkingSafetyBoundary.RealOAuthImplemented);
        Assert.False(GreeAliceYandexAccountLinkingSafetyBoundary.AllowsDeviceControl);
        Assert.False(GreeAliceYandexAccountLinkingSafetyBoundary.AllowsMqtt);
        Assert.False(GreeAliceYandexAccountLinkingSafetyBoundary.ProductionWiringAllowed);
    }

    [Fact]
    public void AccountLinkingDocsExistAndExplainFailClosedScopedFlow()
    {
        string boundary = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-account-linking-boundary.md");
        string template = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-account-linking-flow-template.md");

        Assert.Contains("Alice receives devices after account linking", boundary, StringComparison.Ordinal);
        Assert.Contains("bridge account must map to an explicit registry scope", boundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unknown/unlinked users must receive no devices or fail-closed behavior", boundary, StringComparison.Ordinal);
        Assert.Contains("Real OAuth is not implemented in this stage", boundary, StringComparison.Ordinal);
        Assert.Contains("Real Yandex production app credentials are not stored in repo", boundary, StringComparison.Ordinal);
        Assert.Contains("Real tokens are not issued", boundary, StringComparison.Ordinal);
        Assert.Contains("No live Gree+ Cloud integration in this stage", boundary, StringComparison.Ordinal);
        Assert.Contains("No MQTT", boundary, StringComparison.Ordinal);
        Assert.Contains("No device control", boundary, StringComparison.Ordinal);
        Assert.Contains("No production wiring", boundary, StringComparison.Ordinal);
        Assert.Contains("Step 1: User Selects AssistantEngineer/Gree Integration In Alice App", template, StringComparison.Ordinal);
        Assert.Contains("Step 9: User Can Unlink Integration", template, StringComparison.Ordinal);
        AssertNoMacLikeValue(boundary + template);
        Assert.DoesNotContain("clientSecret", boundary + template, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", boundary + template, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", boundary + template, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GreeAliceBridgeSourceHasNoLiveOAuthNetworkControlOrProductionWiring()
    {
        string source = NormalizeAllowedBoundaryTerms(ReadBridgeSource());
        string bridgeProjects = ReadBridgeProjects();
        string docsRoot = Path.Combine(FindRepositoryRoot(), "docs", "integrations", "gree-alice");

        Assert.DoesNotContain("MapGet(\"/oauth", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MapPost(\"/oauth", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorizationCode", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", source, StringComparison.OrdinalIgnoreCase);
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
        Assert.DoesNotContain("password", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deviceKey", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("macAddress", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-yandex-user-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-bridge-account-", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AssistantEngineer.Api.csproj", bridgeProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telegram", bridgeProjects, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Migration", bridgeProjects, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(Directory.EnumerateFiles(docsRoot, "*.csv", SearchOption.AllDirectories));
    }

    private static GreeAliceYandexAccountLinkingTemplate CreateTemplate()
    {
        return new OfflineGreeAliceYandexAccountLinkingTemplateProvider().GetTemplate();
    }

    private static string CombineTemplateText(GreeAliceYandexAccountLinkingTemplate template)
    {
        return string.Join(
            "|",
            template.Session.LinkingSessionId,
            template.Session.YandexUserReference,
            template.Session.BridgeAccountReference,
            template.Session.RegistryScopeReference,
            template.ActiveBinding.YandexUserReference,
            template.ActiveBinding.BridgeAccountReference,
            template.ActiveBinding.RegistryScopeReference,
            template.RegistryScope.BridgeAccountReference,
            template.RegistryScope.RegistryScopeReference,
            string.Join("|", template.RegistryScope.AllowedHomeIds),
            string.Join("|", template.RegistryScope.AllowedDeviceIds),
            string.Join("|", template.RegistryScope.AllowedVrfGatewayIds),
            string.Join("|", template.RegistryScope.AllowedVrfChildUnitIds),
            template.UnlinkRequest.YandexUserReference,
            template.UnlinkResult.Reason);
    }

    private static void AssertNoMacLikeOrRealSecretMaterial(string value)
    {
        AssertNoMacLikeValue(value);
        Assert.DoesNotContain("credential", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clientSecret", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access-token", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-yandex-user-", value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-bridge-account-", value, StringComparison.OrdinalIgnoreCase);
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
            .Replace("RealTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("RefreshTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("AccessTokensIssued", string.Empty, StringComparison.Ordinal)
            .Replace("TokenStorageImplemented", string.Empty, StringComparison.Ordinal)
            .Replace("TokenRevocationImplemented", string.Empty, StringComparison.Ordinal)
            .Replace("AllowsSecretsInRepository", string.Empty, StringComparison.Ordinal)
            .Replace("DeletedSecrets", string.Empty, StringComparison.Ordinal)
            .Replace("DeletedTokens", string.Empty, StringComparison.Ordinal)
            .Replace("ContainsSensitiveMarker", string.Empty, StringComparison.Ordinal)
            .Replace("sensitive material", string.Empty, StringComparison.OrdinalIgnoreCase);
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
