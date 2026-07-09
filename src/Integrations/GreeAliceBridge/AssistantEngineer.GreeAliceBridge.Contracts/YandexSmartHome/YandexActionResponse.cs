using AssistantEngineer.GreeAliceBridge.Contracts;

namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexActionResponse(IReadOnlyList<YandexActionDeviceResultDto> Devices)
{
    public string RequestId { get; init; } = "offline-fixture-action";

    public string Status { get; init; } = "dry-run-fail-closed";

    public string? ErrorCode { get; init; }

    public string Message { get; init; } = "Offline action request was not sent to Gree+ Cloud, MQTT, or a device.";

    public string RuntimeMode { get; init; } = GreeAliceBridgeSafetyBoundary.RuntimeMode;
}
