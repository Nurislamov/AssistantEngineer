namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

public sealed record GreeCloudSingleDeviceControlPilotDryRunResult(
    string PilotStatus,
    bool DryRunOnly,
    bool FailClosed,
    bool WillSendToGreeCloud,
    bool WillSendToMqtt,
    bool WillSendDeviceCommand);
