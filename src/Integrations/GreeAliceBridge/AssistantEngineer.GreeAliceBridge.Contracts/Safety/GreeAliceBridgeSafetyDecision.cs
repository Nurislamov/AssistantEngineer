namespace AssistantEngineer.GreeAliceBridge.Contracts.Safety;

public sealed record GreeAliceBridgeSafetyDecision(
    string Action,
    bool IsAllowed,
    bool IsDryRunOnly,
    bool IsFailClosed,
    string Reason,
    string RuntimeMode);
