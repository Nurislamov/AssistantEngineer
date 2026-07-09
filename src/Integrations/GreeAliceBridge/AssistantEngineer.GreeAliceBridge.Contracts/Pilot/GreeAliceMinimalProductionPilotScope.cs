namespace AssistantEngineer.GreeAliceBridge.Contracts.Pilot;

public sealed record GreeAliceMinimalProductionPilotScope(
    string OperatorId,
    string AccountScope,
    string HomeScope,
    string DeviceScope,
    string VrfChildUnitScope,
    string Mode,
    bool IsMasked,
    bool IsDummyOrTemplate);
