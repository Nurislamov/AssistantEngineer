namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public sealed record GreePlusLiveReadResult(
    GreePlusLiveReadStatus Status,
    GreePlusLiveReadResultReason Reason,
    bool NetworkAttempted,
    IReadOnlyList<string> MissingRequirements,
    IReadOnlyList<string> Diagnostics,
    GreePlusDeviceStatusSnapshot? Snapshot)
{
    public static GreePlusLiveReadResult Blocked(IReadOnlyList<string> missingRequirements)
    {
        return new GreePlusLiveReadResult(
            GreePlusLiveReadStatus.Blocked,
            GreePlusLiveReadResultReason.SafetyGateBlocked,
            NetworkAttempted: false,
            missingRequirements,
            ["read-only probe blocked by safety gate"],
            Snapshot: null);
    }

    public static GreePlusLiveReadResult NotReady(IReadOnlyList<string> missingRequirements)
    {
        return new GreePlusLiveReadResult(
            GreePlusLiveReadStatus.NotReady,
            GreePlusLiveReadResultReason.ContractUnknown,
            NetworkAttempted: false,
            missingRequirements,
            ["exact read-only endpoint and request contract are not confirmed"],
            Snapshot: null);
    }

    public static GreePlusLiveReadResult MissingStatusPayload(IReadOnlyList<string> missingRequirements)
    {
        return new GreePlusLiveReadResult(
            GreePlusLiveReadStatus.NotReady,
            GreePlusLiveReadResultReason.MissingStatusPayload,
            NetworkAttempted: false,
            missingRequirements,
            ["read-only status response payload is required for offline parsing"],
            Snapshot: null);
    }

    public static GreePlusLiveReadResult Parsed(GreePlusDeviceStatusSnapshot snapshot)
    {
        return new GreePlusLiveReadResult(
            GreePlusLiveReadStatus.Parsed,
            GreePlusLiveReadResultReason.OfflineStatusParsed,
            NetworkAttempted: false,
            MissingRequirements: [],
            ["read-only status payload parsed offline"],
            snapshot);
    }
}
