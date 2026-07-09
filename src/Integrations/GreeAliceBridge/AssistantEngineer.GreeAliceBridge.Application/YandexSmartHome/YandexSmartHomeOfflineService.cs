using AssistantEngineer.GreeAliceBridge.Contracts;
using AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

namespace AssistantEngineer.GreeAliceBridge.Application.YandexSmartHome;

public sealed class YandexSmartHomeOfflineService : IYandexSmartHomeOfflineService
{
    private readonly IGreeAliceOfflineBridgeService bridgeService;

    public YandexSmartHomeOfflineService(IGreeAliceOfflineBridgeService bridgeService)
    {
        this.bridgeService = bridgeService;
    }

    public YandexDevicesResponse GetDevices()
    {
        return new YandexDevicesResponse(
            bridgeService.GetDevices().Select(device => new YandexDeviceDto(
                device.Id,
                device.Name,
                device.Room,
                device.Type,
                device.Capabilities.Select(MapCapability).ToArray(),
                device.Online,
                device.Source)).ToArray());
    }

    public YandexQueryResponse QueryDevices(YandexQueryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new YandexQueryResponse(
            request.Devices.Select(device => MapState(bridgeService.QueryDeviceState(device.Id))).ToArray());
    }

    public YandexActionResponse ExecuteAction(YandexActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new YandexActionResponse(
            request.Devices.Select(device =>
            {
                YandexActionCapabilityRequestDto? capability = device.Capabilities.FirstOrDefault();
                GreeAliceActionResult result = bridgeService.ApplyAction(new GreeAliceActionRequest(
                    device.Id,
                    capability?.Type ?? "unknown",
                    capability?.Action ?? "unknown",
                    capability?.Value));

                return new YandexActionDeviceResultDto(
                    result.DeviceId,
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
