namespace AssistantEngineer.GreeAliceBridge.Contracts.Pilot;

public sealed record GreeAliceMinimalProductionPilotDecision(
    string Status,
    string Mode,
    GreeAliceMinimalProductionPilotScope Scope,
    bool ProductionPilotApproved,
    bool ProductionPilotEnabled,
    bool ProductionDeploymentWiringEnabled,
    bool LiveReadOnlyPilotEnabled,
    bool LiveControlEnabled,
    bool MqttAllowed,
    IReadOnlyList<string> UnmetRequirements);
