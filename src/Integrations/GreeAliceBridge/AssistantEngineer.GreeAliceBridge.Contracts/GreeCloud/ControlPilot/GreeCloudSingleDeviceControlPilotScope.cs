namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

public sealed record GreeCloudSingleDeviceControlPilotScope(
    string PilotAccountId,
    string PilotDeviceId,
    string PilotDeviceKind,
    string PilotScopeKind);
