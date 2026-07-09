namespace AssistantEngineer.GreeAliceBridge.Contracts;

public sealed record GreeAliceUnlinkResult(
    string UserId,
    string Status,
    bool ClearedBridgeSessionState,
    bool ClearedProductionAssistantEngineerData);
