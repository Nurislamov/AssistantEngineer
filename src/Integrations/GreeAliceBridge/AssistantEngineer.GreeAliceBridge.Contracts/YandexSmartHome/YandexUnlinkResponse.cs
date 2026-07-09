namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexUnlinkResponse(
    string UserId,
    string Status,
    bool ClearedBridgeSessionState,
    bool ClearedProductionAssistantEngineerData);
