namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

public sealed record GreeCloudLiveReadOnlyPilotGate(
    string Status,
    IReadOnlyList<string> RequiredRequirements)
{
    public static GreeCloudLiveReadOnlyPilotGate Default { get; } = new(
        GreeCloudLiveReadOnlyPilotGateBoundary.PilotGateStatus,
        GreeCloudLiveReadOnlyPilotGateBoundary.RequiredRequirements);
}
