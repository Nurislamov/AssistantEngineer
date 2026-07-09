using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlApproval;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.ControlPilot;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.LiveReadOnly;
using AssistantEngineer.GreeAliceBridge.Application.GreeCloud.Mapping;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Application.Safety;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts;
using AssistantEngineer.GreeAliceBridge.Contracts.Safety;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGreeAliceOfflineBridgeService, OfflineGreeAliceBridgeService>();
builder.Services.AddSingleton<IGreeAliceOfflineRegistryProvider, OfflineGreeAliceRegistryProvider>();
builder.Services.AddSingleton<IGreeCloudReadAdapter, OfflineGreeCloudReadAdapter>();
builder.Services.AddSingleton<IGreeCloudControlAdapter, OfflineGreeCloudControlAdapter>();
builder.Services.AddSingleton<IGreeCloudControlApprovalEvaluator, OfflineGreeCloudControlApprovalEvaluator>();
builder.Services.AddSingleton<IGreeCloudSingleDeviceControlPilotPlanner, OfflineGreeCloudSingleDeviceControlPilotPlanner>();
builder.Services.AddSingleton<IGreeCloudLiveReadOnlyPilotGateEvaluator, OfflineGreeCloudLiveReadOnlyPilotGateEvaluator>();
builder.Services.AddSingleton<IGreeCloudMaskedStateFixtureProvider, OfflineGreeCloudMaskedStateFixtureProvider>();
builder.Services.AddSingleton<IGreeCloudStateMapper, OfflineGreeCloudStateMapper>();
builder.Services.AddSingleton<IGreeAliceBridgeSafetyDecisionService, OfflineGreeAliceBridgeSafetyDecisionService>();
builder.Services.AddSingleton<IYandexSmartHomeOfflineService, YandexSmartHomeOfflineService>();

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

app.MapGet("/v1.0/user/devices", (
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety) =>
{
    GreeAliceBridgeSafetyDecision decision = Decide(safety, GreeAliceBridgeSafetyAction.DiscoverDevices, "devices");

    return decision.IsAllowed
        ? Results.Ok(service.GetDevices())
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/v1.0/user/devices/query", (
    YandexQueryRequest? request,
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety) =>
{
    GreeAliceBridgeSafetyDecision decision = Decide(safety, GreeAliceBridgeSafetyAction.QueryDevices, "query");

    return decision.IsAllowed
        ? Results.Ok(service.QueryDevices(request))
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapPost("/v1.0/user/devices/action", (
    YandexActionRequest? request,
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety) =>
{
    _ = Decide(safety, GreeAliceBridgeSafetyAction.ExecuteAction, "action");

    return Results.Ok(service.ExecuteAction(request));
});

app.MapPost("/v1.0/user/unlink", (
    IYandexSmartHomeOfflineService service,
    IGreeAliceBridgeSafetyDecisionService safety) =>
{
    GreeAliceBridgeSafetyDecision decision = Decide(safety, GreeAliceBridgeSafetyAction.Unlink, "unlink");

    return decision.IsAllowed
        ? Results.Ok(service.Unlink("offline-user"))
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

public partial class Program;
