namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

public sealed record GreeCloudControlPilotDecision(
    string Status,
    bool LiveControlApproved,
    bool LiveControlEnabled,
    bool ControlAdapterEnabled,
    bool ControlAdapterFailClosed,
    bool MqttAllowed,
    bool ProductionWiringAllowed,
    bool SingleDevicePilotApproved,
    IReadOnlyList<string> UnmetRequirements);
