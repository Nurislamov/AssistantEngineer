namespace AssistantEngineer.GreeAliceBridge.Contracts.Safety;

public sealed record GreeAliceBridgeSafetyPolicy(
    string RuntimeMode,
    bool AllowReadOnlyFixtureQueries,
    bool AllowOfflineDeviceDiscovery,
    bool AllowOfflineUnlink,
    bool AllowActionDryRun,
    bool AllowLiveGreeCloud,
    bool AllowLiveHttpNetwork,
    bool AllowMqttConnect,
    bool AllowMqttSubscribe,
    bool AllowMqttPublish,
    bool AllowDeviceControl,
    bool AllowRuntimeControl,
    bool AllowProductionRuntimeWiring)
{
    public static GreeAliceBridgeSafetyPolicy OfflineDefault { get; } = new(
        "offline-fixture",
        AllowReadOnlyFixtureQueries: true,
        AllowOfflineDeviceDiscovery: true,
        AllowOfflineUnlink: true,
        AllowActionDryRun: true,
        AllowLiveGreeCloud: false,
        AllowLiveHttpNetwork: false,
        AllowMqttConnect: false,
        AllowMqttSubscribe: false,
        AllowMqttPublish: false,
        AllowDeviceControl: false,
        AllowRuntimeControl: false,
        AllowProductionRuntimeWiring: false);
}
