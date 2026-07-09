namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

public sealed record GreeCloudSingleDeviceControlPilotCommand(
    string Name,
    bool IsApproved,
    bool DryRunOnly,
    bool WillSendToGreeCloud,
    bool WillSendToMqtt,
    bool WillSendDeviceCommand,
    bool RequiresManualApproval,
    bool RequiresAuditEvent,
    bool RequiresKillSwitchClear);
