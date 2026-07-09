namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public static class GreeAliceRegistryImportBoundary
{
    public const string ImportMode = GreeAliceRegistryImportMode.OfflineTemplate;

    public const bool RealImportEnabled = false;
    public const bool AdminUiImplemented = false;
    public const bool LiveGreeCloudDiscoveryEnabled = false;
    public const bool AutoExposeDiscoveredDevices = false;
    public const bool RequiresManualReview = true;
    public const bool RequiresStableYandexDeviceId = true;
    public const bool RequiresRoomBinding = true;
    public const bool RequiresGatewayChildMappingForVrf = true;
    public const bool AllowsSecretsInImport = false;
    public const bool AllowsMacLikeIdentifiersInRepo = false;
    public const bool AllowsRealAccountIdentifiersInRepo = false;
    public const bool AllowsProductionWrite = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsMqtt = false;
    public const bool ProductionWiringAllowed = false;
}
