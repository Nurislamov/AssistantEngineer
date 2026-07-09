namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry;

public static class GreeAliceRegistrySafetyBoundary
{
    public const bool UsesRealGreeCloudData = false;
    public const bool UsesRealAccountIdentifiers = false;
    public const bool UsesRealDeviceIdentifiers = false;
    public const bool AllowsRuntimeControl = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsMqttConnect = false;
    public const bool AllowsMqttSubscribe = false;
    public const bool AllowsMqttPublish = false;
    public const string RegistryMode = "offline-fixture-registry";
}
