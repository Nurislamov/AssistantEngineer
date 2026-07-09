namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.Mapping;

public static class GreeCloudStateMappingSafetyBoundary
{
    public const string MappingMode = "offline-masked-fixture";
    public const bool UsesMaskedInputOnly = true;
    public const bool UsesLiveGreeCloud = false;
    public const bool UsesHttpNetwork = false;
    public const bool UsesMqttNetwork = false;
    public const bool AllowsMqttConnect = false;
    public const bool AllowsMqttSubscribe = false;
    public const bool AllowsMqttPublish = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsRuntimeControl = false;
    public const bool AllowsRawSecrets = false;
}
