using AssistantEngineer.GreeAliceBridge.Contracts;
using AssistantEngineer.GreeAliceBridge.Application.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.Registry;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome;

public sealed class YandexSmartHomeOfflineService : IYandexSmartHomeOfflineService
{
    private readonly IGreeAliceOfflineBridgeService bridgeService;
    private readonly IGreeAliceOfflineRegistryProvider? registryProvider;

    public YandexSmartHomeOfflineService(
        IGreeAliceOfflineBridgeService bridgeService,
        IGreeAliceOfflineRegistryProvider? registryProvider = null)
    {
        this.bridgeService = bridgeService;
        this.registryProvider = registryProvider;
    }

    public YandexDevicesResponse GetDevices()
    {
        HashSet<string>? exposedSplitDeviceIds = registryProvider
            ?.GetSnapshot()
            .Devices
            .Where(device => device.YandexExposed && string.Equals(device.Kind, GreeAliceDeviceKind.SplitAc, StringComparison.Ordinal))
            .Select(device => device.Id)
            .ToHashSet(StringComparer.Ordinal);

        return new YandexDevicesResponse(
            bridgeService
                .GetDevices()
                .Where(device => exposedSplitDeviceIds is null || exposedSplitDeviceIds.Contains(device.Id))
                .Select(device => new YandexDeviceDto(
                    device.Id,
                    device.Name,
                    device.Room,
                    device.Type,
                    device.Capabilities.Select(MapCapability).ToArray(),
                    device.Online,
                    device.Source)).ToArray());
    }

    public YandexQueryResponse QueryDevices(YandexQueryRequest? request)
    {
        IReadOnlyList<YandexDeviceRequestDto> devices = request?.Devices ?? [];

        if (devices.Count == 0)
        {
            return new YandexQueryResponse([])
            {
                Status = "offline-empty-request",
                ErrorCode = "offline-empty-query",
                Message = "Offline query request did not include devices."
            };
        }

        return new YandexQueryResponse(devices.Select(device => MapState(bridgeService.QueryDeviceState(device.Id))).ToArray());
    }

    public YandexActionResponse ExecuteAction(YandexActionRequest? request)
    {
        IReadOnlyList<YandexActionDeviceRequestDto> devices = request?.Devices ?? [];

        if (devices.Count == 0)
        {
            return new YandexActionResponse([])
            {
                ErrorCode = "offline-empty-action",
                Message = "Offline action request did not include devices; no action was sent."
            };
        }

        return new YandexActionResponse(
            devices.Select(device =>
            {
                YandexActionCapabilityRequestDto? capability = device.Capabilities?.FirstOrDefault();
                string capabilityType = capability?.Type ?? "unknown";
                GreeAliceActionResult result = bridgeService.ApplyAction(new GreeAliceActionRequest(
                    device.Id,
                    capabilityType,
                    capability?.Action ?? "unknown",
                    capability?.Value));

                return new YandexActionDeviceResultDto(
                    result.DeviceId,
                    capabilityType,
                    result.Status,
                    result.SentToGreeCloud,
                    result.SentToMqtt,
                    result.SentToDevice,
                    result.RuntimeMode);
            }).ToArray());
    }

    public YandexUnlinkResponse Unlink(string userId)
    {
        GreeAliceUnlinkResult result = bridgeService.Unlink(userId);

        return new YandexUnlinkResponse(
            result.UserId,
            result.Status,
            result.ClearedBridgeSessionState,
            result.ClearedProductionAssistantEngineerData);
    }

    private static YandexDeviceCapabilityDto MapCapability(string capability)
    {
        return capability switch
        {
            "on_off" => new YandexDeviceCapabilityDto("devices.capabilities.on_off", "on"),
            "mode" => new YandexDeviceCapabilityDto("devices.capabilities.mode", "thermostat"),
            "temperature" => new YandexDeviceCapabilityDto("devices.capabilities.range", "temperature"),
            "fan_speed" => new YandexDeviceCapabilityDto("devices.capabilities.mode", "fan_speed"),
            _ => new YandexDeviceCapabilityDto("devices.capabilities.unknown", capability)
        };
    }

    private static YandexQueryDeviceDto MapState(GreeAliceDeviceState state)
    {
        return new YandexQueryDeviceDto(
            state.DeviceId,
            state.Status,
            state.Online,
            state.On,
            state.Mode,
            state.TargetTemperatureC,
            state.FanSpeed,
            state.Source);
    }
}
