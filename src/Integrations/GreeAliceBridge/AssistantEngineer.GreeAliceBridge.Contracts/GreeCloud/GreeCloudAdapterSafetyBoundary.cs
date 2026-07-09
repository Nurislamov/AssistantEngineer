namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud;

public static class GreeCloudAdapterSafetyBoundary
{
    public const string AdapterMode = GreeCloudAdapterMode.OfflineFake;
    public const bool UsesLiveGreeCloud = false;
    public const bool UsesHttpNetwork = false;
    public const bool UsesMqttNetwork = false;
    public const bool AllowsMqttConnect = false;
    public const bool AllowsMqttSubscribe = false;
    public const bool AllowsMqttPublish = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsRuntimeControl = false;
}
