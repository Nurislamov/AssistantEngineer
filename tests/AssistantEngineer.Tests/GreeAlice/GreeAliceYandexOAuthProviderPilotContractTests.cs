using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Contracts.Pilot;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceYandexOAuthProviderPilotContractTests
{
    [Fact]
    public void PilotContractDocsExistAndAreIndexed()
    {
        AssertRepoFileExists("docs", "integrations", "gree-alice", "yandex-oauth-provider-pilot-contract.md");
        AssertRepoFileExists("docs", "integrations", "gree-alice", "yandex-oauth-provider-pilot-config.example.json");

        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");
        string audit = ReadRepoFile("docs", "integrations", "gree-alice", "release-readiness-audit.md");

        Assert.Contains("yandex-oauth-provider-pilot-contract.md", readme, StringComparison.Ordinal);
        Assert.Contains("yandex-oauth-provider-pilot-config.example.json", readme, StringComparison.Ordinal);
        Assert.Contains("yandex-oauth-provider-pilot-contract.md", audit, StringComparison.Ordinal);
        Assert.Contains("yandex-oauth-provider-pilot-config.example.json", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void PilotContractEncodesOfficialYandexConstraints()
    {
        string contract = ReadContract();

        Assert.Contains("OAuth 2.0 authorization service", contract, StringComparison.Ordinal);
        Assert.Contains("Provider Adapter API", contract, StringComparison.Ordinal);
        Assert.Contains("Yandex Smart Home device format", contract, StringComparison.Ordinal);
        Assert.Contains("X-Request-Id", contract, StringComparison.Ordinal);
        Assert.Contains("Yandex Dialogs", contract, StringComparison.Ordinal);
        Assert.Contains("private skill", contract, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Backend Endpoint URL must use HTTPS", contract, StringComparison.Ordinal);
        Assert.Contains("3 seconds", contract, StringComparison.Ordinal);
        Assert.Contains("2048 characters", contract, StringComparison.Ordinal);
        Assert.Contains("expires_in", contract, StringComparison.Ordinal);
    }

    [Fact]
    public void PilotContractDefinesRequiredDevOnlyEndpoints()
    {
        string contract = ReadContract();

        Assert.Contains("GET /oauth/authorize", contract, StringComparison.Ordinal);
        Assert.Contains("POST /oauth/token", contract, StringComparison.Ordinal);
        Assert.Contains("GET /oauth/callback", contract, StringComparison.Ordinal);
        Assert.Contains("GET /v1.0/user/devices", contract, StringComparison.Ordinal);
        Assert.Contains("POST /v1.0/user/devices/query", contract, StringComparison.Ordinal);
        Assert.Contains("POST /v1.0/user/devices/action", contract, StringComparison.Ordinal);
        Assert.Contains("POST /v1.0/user/unlink", contract, StringComparison.Ordinal);
        Assert.Contains("GET /health", contract, StringComparison.Ordinal);
        Assert.Contains("PILOT-1B endpoints are dev-only/local and in-memory", contract, StringComparison.Ordinal);
        Assert.Contains("Provider endpoints can require Bearer token only in configured `PrivateSkillDevOnly` mode", contract, StringComparison.Ordinal);
    }

    [Fact]
    public void ExampleConfigContainsRequiredKeysAndOnlyPlaceholders()
    {
        string json = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-oauth-provider-pilot-config.example.json");
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement yandex = document.RootElement.GetProperty("GreeAliceBridge").GetProperty("Yandex");

        Assert.Equal("PrivateSkillDevOnly", yandex.GetProperty("PilotMode").GetString());
        Assert.Equal("DummyOfflineDevices", yandex.GetProperty("ProviderMode").GetString());
        Assert.Equal("https://<your-bridge-domain>", yandex.GetProperty("PublicBaseUrl").GetString());
        Assert.Equal("<set-via-secret-store-or-env>", yandex.GetProperty("ClientId").GetString());
        Assert.Equal("<set-via-secret-store-or-env>", yandex.GetProperty("ClientSecret").GetString());
        Assert.Equal("https://<yandex-configured-redirect-uri>", yandex.GetProperty("AllowedRedirectUris")[0].GetString());
        Assert.Equal(300, yandex.GetProperty("AuthorizationCodeLifetimeSeconds").GetInt32());
        Assert.Equal(3600, yandex.GetProperty("AccessTokenLifetimeSeconds").GetInt32());
        Assert.Equal(2592000, yandex.GetProperty("RefreshTokenLifetimeSeconds").GetInt32());
        Assert.True(yandex.GetProperty("RequireHttpsPublicBaseUrl").GetBoolean());
        Assert.True(yandex.GetProperty("EnableDevOnlyInMemoryTokenStore").GetBoolean());
        Assert.Equal("dummy-account-001", yandex.GetProperty("DevOnlyBridgeAccountId").GetString());
        Assert.Equal("masked-yandex-user-dev-001", yandex.GetProperty("DevOnlyMaskedYandexUserId").GetString());

        Assert.DoesNotContain("client_id=", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("client_secret=", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("access_token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh_token", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-account-", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("real-device-", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("grih.gree.com", json, StringComparison.OrdinalIgnoreCase);
        AssertNoMacLikeValue(json);
    }

    [Fact]
    public void PilotContractDocumentsTokenAccountAndAuthenticationRules()
    {
        string contract = ReadContract();

        Assert.Contains("AuthorizationCode", contract, StringComparison.Ordinal);
        Assert.Contains("AccessToken", contract, StringComparison.Ordinal);
        Assert.Contains("RefreshToken", contract, StringComparison.Ordinal);
        Assert.Contains("BridgeAccountId", contract, StringComparison.Ordinal);
        Assert.Contains("MaskedYandexUserId", contract, StringComparison.Ordinal);
        Assert.Contains("Authorization: Bearer <access_token>", contract, StringComparison.Ordinal);
        Assert.Contains("No query-string tokens", contract, StringComparison.Ordinal);
        Assert.Contains("No tokens in logs", contract, StringComparison.Ordinal);
        Assert.Contains("Unknown token fails closed", contract, StringComparison.Ordinal);
        Assert.Contains("Gateway remains hidden", contract, StringComparison.Ordinal);
        Assert.Contains("SentToGreeCloud", contract, StringComparison.Ordinal);
        Assert.Contains("SentToMqtt", contract, StringComparison.Ordinal);
        Assert.Contains("SentToDevice", contract, StringComparison.Ordinal);
    }

    [Fact]
    public void PilotContractKeepsProductionYandexNotReadyAndNextStagePilot1B()
    {
        string contract = ReadContract();
        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");
        string notes = ReadRepoFile("docs", "integrations", "gree-alice", "internal-offline-release-notes-draft.md");

        Assert.Contains("Real Yandex/Alice production release remains NOT READY.", contract, StringComparison.Ordinal);
        Assert.Contains("GREE-ALICE-PILOT-2", contract, StringComparison.Ordinal);
        Assert.Contains("Production Yandex release remains NOT READY.", notes, StringComparison.Ordinal);
        Assert.Contains("Next implementation stage: GREE-ALICE-PILOT-2.", notes, StringComparison.Ordinal);
        Assert.Contains("Dev-only OAuth/provider vertical slice", readme, StringComparison.Ordinal);
        Assert.Equal("not-ready", GreeAliceYandexProviderReadinessBoundary.ProviderReadinessStatus);
        Assert.False(GreeAliceMinimalProductionPilotBoundary.ProductionPilotApproved);
        Assert.Equal("localhost-only", GreeAliceLocalHttpSmokeBoundary.HttpSmokeMode);
    }

    [Fact]
    public void Pilot1BBoundaryConstantsRemainDevOnlyAndFailClosed()
    {
        Assert.Equal("dev-only", GreeAliceYandexOAuthPilotBoundary.RuntimeStatus);
        Assert.Equal("dummy-offline-devices", GreeAliceYandexOAuthPilotBoundary.ProviderMode);
        Assert.Equal("dry-run-fail-closed", GreeAliceYandexOAuthPilotBoundary.ActionMode);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.RealProductionOAuth);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.RealYandexSharedMaterialAllowedInRepository);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.RealTokensAllowedInRepository);
        Assert.True(GreeAliceYandexOAuthPilotBoundary.InMemoryTokenStoreOnly);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.PersistentTokenStoreImplemented);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.LiveGreeReadAllowed);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.LiveGreeControlAllowed);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.MqttAllowed);
        Assert.False(GreeAliceYandexOAuthPilotBoundary.ProductionDeploymentAllowed);
        Assert.Equal(2048, GreeAliceYandexOAuthPilotBoundary.MaxTokenLength);
    }

    [Fact]
    public void PilotContractAddsNoForbiddenRuntimeOrSecretArtifacts()
    {
        string combined = string.Join(
            Environment.NewLine,
            ReadRepoFile("docs", "integrations", "gree-alice", "yandex-oauth-provider-pilot-contract.md"),
            ReadRepoFile("docs", "integrations", "gree-alice", "yandex-oauth-provider-pilot-config.example.json"));

        Assert.DoesNotContain("api.iot.yandex", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("grih.gree.com", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mosquitto", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git push", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker compose up", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kubectl", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Process.Start", combined, StringComparison.OrdinalIgnoreCase);
        AssertNoMacLikeValue(combined);
    }

    private static string ReadContract()
    {
        return ReadRepoFile("docs", "integrations", "gree-alice", "yandex-oauth-provider-pilot-contract.md");
    }

    private static void AssertRepoFileExists(params string[] relativeParts)
    {
        string root = FindRepositoryRoot();
        string path = Path.Combine(new[] { root }.Concat(relativeParts).ToArray());

        Assert.True(File.Exists(path), "Expected repository file to exist: " + path);
    }

    private static void AssertNoMacLikeValue(string value)
    {
        Regex macLike = new(@"(?:[0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}", RegexOptions.Compiled);

        Assert.False(macLike.IsMatch(value), "Value must not look like a hardware identifier: " + value);
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
