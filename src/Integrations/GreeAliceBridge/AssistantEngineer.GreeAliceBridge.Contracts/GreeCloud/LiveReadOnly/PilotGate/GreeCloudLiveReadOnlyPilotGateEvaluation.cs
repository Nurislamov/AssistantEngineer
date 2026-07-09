namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

public sealed record GreeCloudLiveReadOnlyPilotGateEvaluation(
    IReadOnlySet<string> ManuallySatisfiedRequirements);
