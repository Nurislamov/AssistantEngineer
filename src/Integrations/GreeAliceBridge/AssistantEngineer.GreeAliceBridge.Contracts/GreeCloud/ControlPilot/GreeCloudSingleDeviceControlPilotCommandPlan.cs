namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

public sealed record GreeCloudSingleDeviceControlPilotCommandPlan(
    GreeCloudSingleDeviceControlPilotScope Scope,
    IReadOnlyList<GreeCloudSingleDeviceControlPilotCommand> CandidateCommands);
