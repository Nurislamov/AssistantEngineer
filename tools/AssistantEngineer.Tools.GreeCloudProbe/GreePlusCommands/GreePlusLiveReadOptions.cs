namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed record GreePlusLiveReadOptions(
    bool ApproveReadOnly,
    string? LiveReadSwitchValue,
    string? DeviceAlias,
    GreePlusLiveReadConfigSource ConfigSource,
    bool ExactReadContractKnown = false,
    string? ReadOnlyStatusJson = null);
