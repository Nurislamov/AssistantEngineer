using AssistantEngineer.GreeAliceBridge.Contracts;

namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexDevicesResponse(IReadOnlyList<YandexDeviceDto> Devices)
{
    public string RequestId { get; init; } = "offline-fixture-devices";

    public string Status { get; init; } = "ok";

    public string? ErrorCode { get; init; }

    public string Message { get; init; } = "Offline fixture devices response.";

    public string RuntimeMode { get; init; } = GreeAliceBridgeSafetyBoundary.RuntimeMode;
}
