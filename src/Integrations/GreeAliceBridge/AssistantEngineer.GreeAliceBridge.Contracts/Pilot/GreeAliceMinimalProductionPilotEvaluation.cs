namespace AssistantEngineer.GreeAliceBridge.Contracts.Pilot;

public sealed record GreeAliceMinimalProductionPilotEvaluation(
    IReadOnlySet<string> ManuallySatisfiedRequirements);
