namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

public sealed record GreeCloudStateMappingResult(
    GreeCloudNormalizedState State,
    IReadOnlyList<GreeCloudStateMappingIssue> Issues,
    string RuntimeMode);
