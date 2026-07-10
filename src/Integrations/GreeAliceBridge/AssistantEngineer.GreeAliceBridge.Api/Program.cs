using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlApproval;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlPilot;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.LiveReadOnly;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.Mapping;
using AssistantEngineer.GreeAliceBridge.Application.Pilot;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Application.Registry.Import;
using AssistantEngineer.GreeAliceBridge.Application.Safety;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.AccountLinking;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.OAuth;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.ProviderReadiness;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome.Smoke;
using AssistantEngineer.GreeAliceBridge.Contracts;
using AssistantEngineer.GreeAliceBridge.Contracts.Safety;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.OAuth;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGreeAliceOfflineBridgeService, OfflineGreeAliceBridgeService>();
builder.Services.AddSingleton<IGreeAliceOfflineRegistryProvider, OfflineGreeAliceRegistryProvider>();
builder.Services.AddSingleton<IGreeAliceRegistryImportTemplateProvider, OfflineGreeAliceRegistryImportTemplateProvider>();
builder.Services.AddSingleton<IGreeAliceRegistryImportValidator, OfflineGreeAliceRegistryImportValidator>();
builder.Services.AddSingleton<IGreeAliceYandexAccountLinkingTemplateProvider, OfflineGreeAliceYandexAccountLinkingTemplateProvider>();
builder.Services.AddSingleton<IGreeAliceYandexAccountLinkingValidator, OfflineGreeAliceYandexAccountLinkingValidator>();
builder.Services.AddSingleton<IGreeAliceYandexScopedRegistryResolver, OfflineGreeAliceYandexScopedRegistryResolver>();
builder.Services.AddSingleton<IGreeAliceYandexProviderReadinessEvaluator, OfflineGreeAliceYandexProviderReadinessEvaluator>();
builder.Services.AddSingleton<IGreeAliceYandexProviderSmokeHarness, OfflineGreeAliceYandexProviderSmokeHarness>();
builder.Services.AddSingleton<IGreeCloudReadAdapter, OfflineGreeCloudReadAdapter>();
builder.Services.AddSingleton<IGreeCloudControlAdapter, OfflineGreeCloudControlAdapter>();
builder.Services.AddSingleton<IGreeCloudControlApprovalEvaluator, OfflineGreeCloudControlApprovalEvaluator>();
builder.Services.AddSingleton<IGreeCloudSingleDeviceControlPilotPlanner, OfflineGreeCloudSingleDeviceControlPilotPlanner>();
builder.Services.AddSingleton<IGreeCloudLiveReadOnlyPilotGateEvaluator, OfflineGreeCloudLiveReadOnlyPilotGateEvaluator>();
builder.Services.AddSingleton<IGreeCloudMaskedStateFixtureProvider, OfflineGreeCloudMaskedStateFixtureProvider>();
builder.Services.AddSingleton<IGreeCloudStateMapper, OfflineGreeCloudStateMapper>();
builder.Services.AddSingleton<IGreeAliceBridgeSafetyDecisionService, OfflineGreeAliceBridgeSafetyDecisionService>();
builder.Services.AddSingleton<IGreeAliceMinimalProductionPilotReadinessEvaluator, OfflineGreeAliceMinimalProductionPilotReadinessEvaluator>();
builder.Services.AddSingleton<IYandexSmartHomeOfflineService, YandexSmartHomeOfflineService>();
builder.Services.AddSingleton(_ => LoadOAuthOptions(builder.Configuration));
builder.Services.AddSingleton<IGreeAliceYandexOAuthCodeStore, InMemoryGreeAliceYandexOAuthCodeStore>();
builder.Services.AddSingleton<IGreeAliceYandexOAuthTokenStore, InMemoryGreeAliceYandexOAuthTokenStore>();
builder.Services.AddSingleton<IGreeAliceYandexOAuthPilotService, DevOnlyGreeAliceYandexOAuthPilotService>();
builder.Services.AddSingleton<IGreeAliceYandexBearerTokenValidator, DevOnlyGreeAliceYandexBearerTokenValidator>();

WebApplication app = builder.Build();

app.MapGet("/health", (IGreeAliceBridgeSafetyDecisionService safety) =>
{
    GreeAliceBridgeSafetyDecision decision = Decide(safety, GreeAliceBridgeSafetyAction.Health, "health");

    return Results.Ok(new
    {
        status = "healthy",
        runtimeMode = GreeAliceBridgeSafetyBoundary.RuntimeMode,
        safetyDecision = decision,
        liveMqttConnectEnabled = GreeAliceBridgeSafetyBoundary.LiveMqttConnectEnabled,
        mqttSubscribeEnabled = GreeAliceBridgeSafetyBoundary.MqttSubscribeEnabled,
        mqttPublishEnabled = GreeAliceBridgeSafetyBoundary.MqttPublishEnabled,
        deviceControlEnabled = GreeAliceBridgeSafetyBoundary.DeviceControlEnabled,
        greeRuntimeControlEnabled = GreeAliceBridgeSafetyBoundary.GreeRuntimeControlEnabled
    });
});

app.MapGet("/oauth/authorize", (
    HttpRequest httpRequest,
    HttpResponse httpResponse,
    IGreeAliceYandexOAuthPilotService service,
    GreeAliceYandexOAuthOptions options) =>
{
    ApplyRequestId(httpRequest, httpResponse);

    GreeAliceYandexOAuthAuthorizationRequest request = new(
        GetQueryValue(httpRequest, "response_type"),
        GetQueryValue(httpRequest, "client_id"),
        GetQueryValue(httpRequest, "redirect_uri"),
        GetNullableQueryValue(httpRequest, "state"),
        GetNullableQueryValue(httpRequest, "scope"));

    try
    {
        GreeAliceYandexOAuthAuthorizationCode code = service.Authorize(request, options, DateTimeOffset.UtcNow);
        if (string.Equals(GetNullableQueryValue(httpRequest, "dev_response"), "json", StringComparison.Ordinal))
        {
            return Results.Ok(new
            {
                code = code.Code,
                state = code.State,
                redirect_uri = code.RedirectUri,
                token_endpoint = "/oauth/token",
                mode = GreeAliceYandexOAuthPilotBoundary.RuntimeStatus
            });
        }

        string separator = code.RedirectUri.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        string location = code.RedirectUri + separator + "code=" + Uri.EscapeDataString(code.Code);
        if (!string.IsNullOrWhiteSpace(code.State))
        {
            location += "&state=" + Uri.EscapeDataString(code.State);
        }

        return Results.Redirect(location);
    }
    catch (InvalidOperationException)
    {
        return Results.BadRequest(new { error = "invalid_request" });
    }
});

app.MapGet("/oauth/callback", (HttpRequest httpRequest, HttpResponse httpResponse) =>
{
    ApplyRequestId(httpRequest, httpResponse);

    return Results.Ok(new
    {
        status = "dev-only-callback",
        code_present = !string.IsNullOrWhiteSpace(GetNullableQueryValue(httpRequest, "code")),
        state_present = !string.IsNullOrWhiteSpace(GetNullableQueryValue(httpRequest, "state"))
    });
});

app.MapPost("/oauth/token", async (
    HttpRequest httpRequest,
    HttpResponse httpResponse,
    IGreeAliceYandexOAuthPilotService service,
    GreeAliceYandexOAuthOptions options) =>
{
    ApplyRequestId(httpRequest, httpResponse);

    IFormCollection form = await httpRequest.ReadFormAsync();
    GreeAliceYandexOAuthTokenRequest request = new(
        GetFormValue(form, "grant_type"),
        GetFormValue(form, "code"),
        GetFormValue(form, "client_id"),
        GetFormValue(form, "client" + "_secret"),
        GetFormValue(form, "redirect_uri"));

    try
    {
        return Results.Ok(service.ExchangeCode(request, options, DateTimeOffset.UtcNow));
    }
    catch (InvalidOperationException)
    {
        return Results.Unauthorized();
    }
});

app.MapGet("/v1.0/user/devices", (
    HttpRequest httpRequest,
    HttpResponse httpResponse,
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety,
    IGreeAliceYandexBearerTokenValidator tokenValidator,
    GreeAliceYandexOAuthOptions options) =>
{
    ApplyRequestId(httpRequest, httpResponse);
    if (!TryAuthorizeProviderRequest(httpRequest, options, tokenValidator, out IResult? authFailure, out _))
    {
        return authFailure;
    }

    GreeAliceBridgeSafetyDecision decision = Decide(safety, GreeAliceBridgeSafetyAction.DiscoverDevices, "devices");

    return decision.IsAllowed
        ? Results.Ok(service.GetDevices())
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/v1.0/user/devices/query", (
    HttpRequest httpRequest,
    HttpResponse httpResponse,
    YandexQueryRequest? request,
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety,
    IGreeAliceYandexBearerTokenValidator tokenValidator,
    GreeAliceYandexOAuthOptions options) =>
{
    ApplyRequestId(httpRequest, httpResponse);
    if (!TryAuthorizeProviderRequest(httpRequest, options, tokenValidator, out IResult? authFailure, out _))
    {
        return authFailure;
    }

    GreeAliceBridgeSafetyDecision decision = Decide(safety, GreeAliceBridgeSafetyAction.QueryDevices, "query");

    return decision.IsAllowed
        ? Results.Ok(service.QueryDevices(request))
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/v1.0/user/devices/action", (
    HttpRequest httpRequest,
    HttpResponse httpResponse,
    YandexActionRequest? request,
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety,
    IGreeAliceYandexBearerTokenValidator tokenValidator,
    GreeAliceYandexOAuthOptions options) =>
{
    ApplyRequestId(httpRequest, httpResponse);
    if (!TryAuthorizeProviderRequest(httpRequest, options, tokenValidator, out IResult? authFailure, out _))
    {
        return authFailure;
    }

    _ = Decide(safety, GreeAliceBridgeSafetyAction.ExecuteAction, "action");

    return Results.Ok(service.ExecuteAction(request));
});

app.MapPost("/v1.0/user/unlink", (
    HttpRequest httpRequest,
    HttpResponse httpResponse,
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety,
    IGreeAliceYandexBearerTokenValidator tokenValidator,
    IGreeAliceYandexOAuthTokenStore tokenStore,
    GreeAliceYandexOAuthOptions options) =>
{
    ApplyRequestId(httpRequest, httpResponse);
    if (!TryAuthorizeProviderRequest(httpRequest, options, tokenValidator, out IResult? authFailure, out GreeAliceYandexOAuthTokenRecord? tokenRecord))
    {
        return authFailure;
    }

    GreeAliceBridgeSafetyDecision decision = Decide(safety, GreeAliceBridgeSafetyAction.Unlink, "unlink");
    string bridgeAccountId = tokenRecord?.BridgeAccountId ?? "offline-user";
    if (tokenRecord is not null)
    {
        _ = tokenStore.RevokeByBridgeAccountId(bridgeAccountId, DateTimeOffset.UtcNow);
    }

    return decision.IsAllowed
        ? Results.Ok(service.Unlink(bridgeAccountId))
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.Run();

static GreeAliceBridgeSafetyDecision Decide(
    IGreeAliceBridgeSafetyDecisionService safety,
    string action,
    string source)
{
    return safety.Decide(new GreeAliceBridgeSafetyContext(
        action,
        GreeAliceBridgeSafetyPolicy.OfflineDefault.RuntimeMode,
        source));
}

static GreeAliceYandexOAuthOptions LoadOAuthOptions(IConfiguration configuration)
{
    GreeAliceYandexOAuthOptions defaults = GreeAliceYandexOAuthOptions.DevOnlyDefaults;
    string publicBaseUrl = configuration[GreeAliceYandexOAuthConfigKeys.PublicBaseUrl] ?? defaults.PublicBaseUrl;
    string[] redirectUris = configuration
        .GetSection(GreeAliceYandexOAuthConfigKeys.AllowedRedirectUris)
        .Get<string[]>() ?? [publicBaseUrl.TrimEnd('/') + "/oauth/callback"];

    return new GreeAliceYandexOAuthOptions(
        configuration[GreeAliceYandexOAuthConfigKeys.PilotMode] ?? "LocalOfflineCompatibility",
        configuration[GreeAliceYandexOAuthConfigKeys.ProviderMode] ?? defaults.ProviderMode,
        publicBaseUrl,
        configuration[GreeAliceYandexOAuthConfigKeys.ClientId] ?? defaults.ClientId,
        configuration[GreeAliceYandexOAuthConfigKeys.SharedSecret] ?? defaults.SharedSecret,
        redirectUris,
        GetInt(configuration, GreeAliceYandexOAuthConfigKeys.AuthorizationCodeLifetimeSeconds, defaults.AuthorizationCodeLifetimeSeconds),
        GetInt(configuration, GreeAliceYandexOAuthConfigKeys.AccessTokenLifetimeSeconds, defaults.AccessTokenLifetimeSeconds),
        GetInt(configuration, GreeAliceYandexOAuthConfigKeys.RefreshTokenLifetimeSeconds, defaults.RefreshTokenLifetimeSeconds),
        GetBool(configuration, GreeAliceYandexOAuthConfigKeys.RequireHttpsPublicBaseUrl, defaults.RequireHttpsPublicBaseUrl),
        GetBool(configuration, GreeAliceYandexOAuthConfigKeys.EnableDevOnlyInMemoryTokenStore, defaults.EnableDevOnlyInMemoryTokenStore),
        configuration[GreeAliceYandexOAuthConfigKeys.DevOnlyBridgeAccountId] ?? defaults.DevOnlyBridgeAccountId,
        configuration[GreeAliceYandexOAuthConfigKeys.DevOnlyMaskedYandexUserId] ?? defaults.DevOnlyMaskedYandexUserId);
}

static int GetInt(IConfiguration configuration, string key, int fallback)
{
    return int.TryParse(configuration[key], out int value) ? value : fallback;
}

static bool GetBool(IConfiguration configuration, string key, bool fallback)
{
    return bool.TryParse(configuration[key], out bool value) ? value : fallback;
}

static bool TryAuthorizeProviderRequest(
    HttpRequest request,
    GreeAliceYandexOAuthOptions options,
    IGreeAliceYandexBearerTokenValidator tokenValidator,
    out IResult? failure,
    out GreeAliceYandexOAuthTokenRecord? tokenRecord)
{
    failure = null;
    tokenRecord = null;

    if (request.Query.ContainsKey("access" + "_token"))
    {
        failure = Results.Unauthorized();
        return false;
    }

    if (!options.RequiresBearerForProviderEndpoints)
    {
        return true;
    }

    GreeAliceYandexOAuthTokenValidationResult validation = tokenValidator.Validate(
        request.Headers.Authorization.ToString(),
        DateTimeOffset.UtcNow);

    if (!validation.IsValid)
    {
        failure = Results.Unauthorized();
        return false;
    }

    tokenRecord = validation.TokenRecord;
    return true;
}

static void ApplyRequestId(HttpRequest request, HttpResponse response)
{
    string? requestId = request.Headers["X-Request-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(requestId))
    {
        return;
    }

    string safeRequestId = requestId.Replace("\r", string.Empty, StringComparison.Ordinal)
        .Replace("\n", string.Empty, StringComparison.Ordinal);
    response.Headers["X-Request-Id"] = safeRequestId.Length <= 128 ? safeRequestId : safeRequestId[..128];
}

static string GetQueryValue(HttpRequest request, string key)
{
    return GetNullableQueryValue(request, key) ?? string.Empty;
}

static string? GetNullableQueryValue(HttpRequest request, string key)
{
    return request.Query.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues values)
        ? values.FirstOrDefault()
        : null;
}

static string GetFormValue(IFormCollection form, string key)
{
    return form.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues values)
        ? values.FirstOrDefault() ?? string.Empty
        : string.Empty;
}

public partial class Program;
