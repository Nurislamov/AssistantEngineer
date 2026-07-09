namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

public sealed record GreeCloudNormalizedState(
    string DeviceId,
    bool IsKnownDevice,
    bool IsOnline,
    bool? IsOn,
    string? Mode,
    int? TargetTemperatureC,
    int? CurrentTemperatureC,
    string? FanSpeed,
    string? SwingVertical,
    string? SwingHorizontal,
    string UpdatedBy,
    string RuntimeMode,
    string SourceKind,
    IReadOnlyList<GreeCloudStateMappingIssue> Issues);
