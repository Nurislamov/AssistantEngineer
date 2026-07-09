using AssistantEngineer.GreeAliceBridge.Contracts;

public sealed record YandexUnlinkResponse(
    string UserId,
    string Status,
    bool ClearedBridgeSessionState,
    bool ClearedProductionAssistantEngineerData)
{
    public string RequestId { get; init; } = "offline-fixture-unlink";

    public string? ErrorCode { get; init; }

    public string Message { get; init; } = "Offline unlink result did not touch production AssistantEngineer data.";

    public string RuntimeMode { get; init; } = GreeAliceBridgeSafetyBoundary.RuntimeMode;
}
