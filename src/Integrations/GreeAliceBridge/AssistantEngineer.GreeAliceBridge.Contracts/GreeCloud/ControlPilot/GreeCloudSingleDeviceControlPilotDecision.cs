namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

public sealed record GreeCloudSingleDeviceControlPilotDecision(
    string PilotStatus,
    GreeCloudSingleDeviceControlPilotScope Scope,
    GreeCloudSingleDeviceControlPilotCommandPlan CommandPlan,
    GreeCloudSingleDeviceControlPilotDryRunResult DryRunResult,
    bool LiveControlEnabled,
    bool MqttAllowed,
    bool ProductionWiringAllowed);
