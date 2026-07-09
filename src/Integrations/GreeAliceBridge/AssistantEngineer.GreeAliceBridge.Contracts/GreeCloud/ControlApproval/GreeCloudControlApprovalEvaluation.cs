namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

public sealed record GreeCloudControlApprovalEvaluation(
    IReadOnlySet<string> ManuallySatisfiedRequirements);
