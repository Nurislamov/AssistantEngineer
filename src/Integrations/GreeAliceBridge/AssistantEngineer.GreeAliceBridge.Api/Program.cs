using AssistantEngineer.GreeAliceBridge.Application;
using AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome;
using AssistantEngineer.GreeAliceBridge.Contracts;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IGreeAliceOfflineBridgeService, OfflineGreeAliceBridgeService>();
builder.Services.AddSingleton<IYandexSmartHomeOfflineService, YandexSmartHomeOfflineService>();

WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    runtimeMode = GreeAliceBridgeSafetyBoundary.RuntimeMode,
    liveMqttConnectEnabled = GreeAliceBridgeSafetyBoundary.LiveMqttConnectEnabled,
    mqttSubscribeEnabled = GreeAliceBridgeSafetyBoundary.MqttSubscribeEnabled,
    mqttPublishEnabled = GreeAliceBridgeSafetyBoundary.MqttPublishEnabled,
    deviceControlEnabled = GreeAliceBridgeSafetyBoundary.DeviceControlEnabled,
    greeRuntimeControlEnabled = GreeAliceBridgeSafetyBoundary.GreeRuntimeControlEnabled
}));

app.MapGet("/v1.0/user/devices", (IYandexSmartHomeOfflineService service) => Results.Ok(service.GetDevices()));

app.MapPost("/v1.0/user/devices/query", (
    YandexQueryRequest request,
    IYandexSmartHomeOfflineService service) => Results.Ok(service.QueryDevices(request)));

app.MapPost("/v1.0/user/devices/action", (
    YandexActionRequest request,
    IYandexSmartHomeOfflineService service) => Results.Ok(service.ExecuteAction(request)));

app.MapPost("/v1.0/user/unlink", (IYandexSmartHomeOfflineService service) =>
    Results.Ok(service.Unlink("offline-user")));

app.Run();

public partial class Program;
