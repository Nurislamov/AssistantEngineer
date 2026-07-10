extern alias GreeAliceBridgeApi;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Tests.GreeAlice;

public sealed class GreeAliceYandexOAuthProviderPilotVerticalSliceTests
{
    private const string LocalBaseUrl = "http://localhost:5005";
    private const string RedirectUri = LocalBaseUrl + "/oauth/callback";

    [Fact]
    public void InMemoryCodeStoreConsumesCodeOnlyOnce()
    {
        InMemoryGreeAliceYandexOAuthCodeStore store = new();
        GreeAliceYandexOAuthOptions options = GreeAliceYandexOAuthOptions.DevOnlyDefaults;
        DateTimeOffset now = new(2026, 7, 10, 8, 0, 0, TimeSpan.Zero);
        GreeAliceYandexOAuthAuthorizationRequest request = new("code", options.ClientId, options.AllowedRedirectUris[0], "state-001", "devices");

        GreeAliceYandexOAuthAuthorizationCode code = store.Create(request, options, now);
        GreeAliceYandexOAuthAuthorizationCode? consumed = store.Consume(code.Code, options.ClientId, options.AllowedRedirectUris[0], now.AddSeconds(1));
        GreeAliceYandexOAuthAuthorizationCode? reused = store.Consume(code.Code, options.ClientId, options.AllowedRedirectUris[0], now.AddSeconds(2));

        Assert.NotNull(consumed);
        Assert.Null(reused);
        Assert.Null(store.Consume(code.Code, "wrong-client", options.AllowedRedirectUris[0], now.AddSeconds(1)));
        Assert.Null(store.Consume(code.Code, options.ClientId, "http://localhost:5005/wrong", now.AddSeconds(1)));
    }

    [Fact]
    public void DevOnlyServiceExchangesCodeAndRejectsReuseOrWrongSecret()
    {
        InMemoryGreeAliceYandexOAuthCodeStore codeStore = new();
        InMemoryGreeAliceYandexOAuthTokenStore tokenStore = new();
        DevOnlyGreeAliceYandexOAuthPilotService service = new(codeStore, tokenStore);
        GreeAliceYandexOAuthOptions options = GreeAliceYandexOAuthOptions.DevOnlyDefaults;
        DateTimeOffset now = new(2026, 7, 10, 8, 0, 0, TimeSpan.Zero);

        GreeAliceYandexOAuthAuthorizationCode code = service.Authorize(
            new GreeAliceYandexOAuthAuthorizationRequest("code", options.ClientId, options.AllowedRedirectUris[0], "state-001", "devices"),
            options,
            now);
        GreeAliceYandexOAuthTokenResponse response = service.ExchangeCode(
            new GreeAliceYandexOAuthTokenRequest("authorization_code", code.Code, options.ClientId, options.SharedSecret, options.AllowedRedirectUris[0]),
            options,
            now.AddSeconds(1));

        Assert.Equal("Bearer", response.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.True(response.AccessToken.Length <= GreeAliceYandexOAuthPilotBoundary.MaxTokenLength);
        Assert.True(tokenStore.ValidateAccessToken(response.AccessToken, now.AddSeconds(2)).IsValid);
        Assert.Throws<InvalidOperationException>(() => service.ExchangeCode(
            new GreeAliceYandexOAuthTokenRequest("authorization_code", code.Code, options.ClientId, options.SharedSecret, options.AllowedRedirectUris[0]),
            options,
            now.AddSeconds(2)));

        GreeAliceYandexOAuthAuthorizationCode secondCode = service.Authorize(
            new GreeAliceYandexOAuthAuthorizationRequest("code", options.ClientId, options.AllowedRedirectUris[0], "state-002", "devices"),
            options,
            now.AddSeconds(3));

        Assert.Throws<InvalidOperationException>(() => service.ExchangeCode(
            new GreeAliceYandexOAuthTokenRequest("authorization_code", secondCode.Code, options.ClientId, "wrong-dev-value", options.AllowedRedirectUris[0]),
            options,
            now.AddSeconds(4)));
    }

    [Fact]
    public async Task ApiOAuthFlowRequiresBearerInPilotModeAndKeepsActionFailClosed()
    {
        await using WebApplicationFactory<GreeAliceBridgeApi::Program> factory = CreatePilotFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage denied = await client.GetAsync("/v1.0/user/devices");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);

        string issuedAccessValue = await IssueBearerValue(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issuedAccessValue);
        client.DefaultRequestHeaders.Add("X-Request-Id", "pilot-1b-test-request");

        HttpResponseMessage devicesResponse = await client.GetAsync("/v1.0/user/devices");
        Assert.True(devicesResponse.IsSuccessStatusCode);
        Assert.True(devicesResponse.Headers.TryGetValues("X-Request-Id", out IEnumerable<string>? echoedIds));
        Assert.Contains("pilot-1b-test-request", echoedIds);

        YandexDevicesResponse? devices = await devicesResponse.Content.ReadFromJsonAsync<YandexDevicesResponse>();
        Assert.NotNull(devices);
        Assert.Contains(devices!.Devices, device => device.Id == "dummy-gree-ac-001");
        Assert.DoesNotContain(devices.Devices, device => device.Id == "dummy-vrf-gateway-001");

        YandexQueryResponse? query = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/query",
            new YandexQueryRequest([
                new YandexDeviceRequestDto("dummy-gree-ac-001"),
                new YandexDeviceRequestDto("unknown-device-001")
            ]))).Content.ReadFromJsonAsync<YandexQueryResponse>();
        Assert.Contains(query!.Devices, device => device.Status == "offline-fixture");

        YandexActionResponse? action = await (await client.PostAsJsonAsync(
            "/v1.0/user/devices/action",
            new YandexActionRequest([
                new YandexActionDeviceRequestDto("dummy-gree-ac-001", [])
            ]))).Content.ReadFromJsonAsync<YandexActionResponse>();
        YandexActionDeviceResultDto actionResult = Assert.Single(action!.Devices);
        Assert.Equal("dry-run-fail-closed", actionResult.Status);
        Assert.False(actionResult.SentToGreeCloud);
        Assert.False(actionResult.SentToMqtt);
        Assert.False(actionResult.SentToDevice);

        YandexUnlinkResponse? unlink = await (await client.PostAsync("/v1.0/user/unlink", content: null))
            .Content.ReadFromJsonAsync<YandexUnlinkResponse>();
        Assert.Equal("offline-no-production-data-touched", unlink!.Status);

        HttpResponseMessage afterUnlink = await client.GetAsync("/v1.0/user/devices");
        Assert.Equal(HttpStatusCode.Unauthorized, afterUnlink.StatusCode);
    }

    [Fact]
    public void DevSmokeDocsAndScriptDeclareLocalOnlyOAuthBoundary()
    {
        string devSmoke = ReadRepoFile("docs", "integrations", "gree-alice", "yandex-oauth-provider-dev-smoke.md");
        string readme = ReadRepoFile("docs", "integrations", "gree-alice", "README.md");
        string script = ReadRepoFile("scripts", "integrations", "gree-alice", "run-local-yandex-provider-smoke.ps1");
        string combinedDocs = devSmoke + Environment.NewLine + readme;

        Assert.Contains("Dev-only/local only.", devSmoke, StringComparison.Ordinal);
        Assert.Contains("Does not call Yandex live endpoints.", devSmoke, StringComparison.Ordinal);
        Assert.Contains("Does not call Gree+ live endpoints.", devSmoke, StringComparison.Ordinal);
        Assert.Contains("Does not use MQTT.", devSmoke, StringComparison.Ordinal);
        Assert.Contains("Does not control devices.", devSmoke, StringComparison.Ordinal);
        Assert.Contains("Production Yandex release remains NOT READY.", devSmoke, StringComparison.Ordinal);
        Assert.Contains("yandex-oauth-provider-dev-smoke.md", readme, StringComparison.Ordinal);
        Assert.Contains("[switch]$RunOAuthSmoke", script, StringComparison.Ordinal);
        Assert.Contains("function Invoke-LocalOAuthSmoke", script, StringComparison.Ordinal);
        Assert.Contains("LocalBaseUrl host must be localhost or 127.0.0.1 only.", script, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $issuedAccessValue", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api.iot.yandex", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("grih.gree.com", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("git push", script, StringComparison.OrdinalIgnoreCase);
        AssertNoMacLikeValue(combinedDocs);
    }

    private static WebApplicationFactory<GreeAliceBridgeApi::Program> CreatePilotFactory()
    {
        return new WebApplicationFactory<GreeAliceBridgeApi::Program>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GreeAliceBridge:Yandex:PilotMode"] = "PrivateSkillDevOnly",
                    ["GreeAliceBridge:Yandex:PublicBaseUrl"] = LocalBaseUrl,
                    ["GreeAliceBridge:Yandex:AllowedRedirectUris:0"] = RedirectUri,
                    ["GreeAliceBridge:Yandex:ClientId"] = "dev-yandex-client",
                    ["GreeAliceBridge:Yandex:ClientSecret"] = "dev-yandex-client-secret",
                    ["GreeAliceBridge:Yandex:DevOnlyBridgeAccountId"] = "dummy-account-001",
                    ["GreeAliceBridge:Yandex:DevOnlyMaskedYandexUserId"] = "masked-yandex-user-dev-001"
                });
            }));
    }

    private static async Task<string> IssueBearerValue(HttpClient client)
    {
        string authorizePath = "/oauth/authorize?response_type=code&client_id=dev-yandex-client&redirect_uri=" +
            Uri.EscapeDataString(RedirectUri) + "&state=state-001&dev_response=json";
        using JsonDocument authorization = JsonDocument.Parse(await client.GetStringAsync(authorizePath));
        string code = authorization.RootElement.GetProperty("code").GetString()!;

        using FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = "dev-yandex-client",
            ["client_secret"] = "dev-yandex-client-secret",
            ["redirect_uri"] = RedirectUri
        });

        using HttpResponseMessage tokenResponse = await client.PostAsync("/oauth/token", form);
        Assert.True(tokenResponse.IsSuccessStatusCode, await tokenResponse.Content.ReadAsStringAsync());
        using JsonDocument tokenJson = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        string issuedAccessValue = tokenJson.RootElement.GetProperty("access_token").GetString()!;

        Assert.False(string.IsNullOrWhiteSpace(issuedAccessValue));
        Assert.DoesNotContain("dev-yandex-client-secret", issuedAccessValue, StringComparison.Ordinal);

        return issuedAccessValue;
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
