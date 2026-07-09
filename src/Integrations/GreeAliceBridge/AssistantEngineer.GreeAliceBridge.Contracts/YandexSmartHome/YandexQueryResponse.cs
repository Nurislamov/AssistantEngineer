using AssistantEngineer.GreeAliceBridge.Contracts;

namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexQueryResponse(IReadOnlyList<YandexQueryDeviceDto> Devices)
{
    public string RequestId { get; init; } = "offline-fixture-query";

    public string Status { get; init; } = "ok";

    public string? ErrorCode { get; init; }

    public string Message { get; init; } = "Offline fixture query response.";

    public string RuntimeMode { get; init; } = GreeAliceBridgeSafetyBoundary.RuntimeMode;
}
