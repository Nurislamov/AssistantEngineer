namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

public sealed record GreeCloudStateMappingIssue(
    string Code,
    string? FieldName,
    string Message);
