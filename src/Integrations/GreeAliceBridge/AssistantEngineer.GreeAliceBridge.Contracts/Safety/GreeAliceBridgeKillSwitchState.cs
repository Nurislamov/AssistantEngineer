namespace AssistantEngineer.GreeAliceBridge.Contracts.Safety;

public sealed record GreeAliceBridgeKillSwitchState(
    bool GlobalBridgeDisabled,
    bool DiscoveryDisabled,
    bool QueryDisabled,
    bool ActionDisabled,
    bool UnlinkDisabled,
    bool ReadAdapterDisabled,
    bool ControlAdapterDisabled)
{
    public static GreeAliceBridgeKillSwitchState OfflineDefault { get; } = new(
        GlobalBridgeDisabled: false,
        DiscoveryDisabled: false,
        QueryDisabled: false,
        ActionDisabled: false,
        UnlinkDisabled: false,
        ReadAdapterDisabled: false,
        ControlAdapterDisabled: true);
}
