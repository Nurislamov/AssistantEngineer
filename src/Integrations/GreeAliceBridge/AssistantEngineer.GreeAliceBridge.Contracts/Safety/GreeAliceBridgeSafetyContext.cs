namespace AssistantEngineer.GreeAliceBridge.Contracts.Safety;

public sealed record GreeAliceBridgeSafetyContext(
    string Action,
    string RuntimeMode,
    string Source);
