namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

public sealed record GreeCloudLiveReadOnlyPilotGateDecision(
    string Status,
    bool PilotGateOpen,
    bool LiveReadOnlyPilotAllowed,
    bool LiveReadOnlyAdapterEnabled,
    bool LiveControlAllowed,
    bool MqttAllowed,
    bool ProductionWiringAllowed,
    IReadOnlyList<string> UnmetRequirements);
