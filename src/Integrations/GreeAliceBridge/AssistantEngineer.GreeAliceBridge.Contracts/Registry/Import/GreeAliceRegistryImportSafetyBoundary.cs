namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Import;

public static class GreeAliceRegistryImportSafetyBoundary
{
    public const bool RealImportEnabled = GreeAliceRegistryImportBoundary.RealImportEnabled;
    public const bool AdminUiImplemented = GreeAliceRegistryImportBoundary.AdminUiImplemented;
    public const bool LiveGreeCloudDiscoveryEnabled = GreeAliceRegistryImportBoundary.LiveGreeCloudDiscoveryEnabled;
    public const bool AutoExposeDiscoveredDevices = GreeAliceRegistryImportBoundary.AutoExposeDiscoveredDevices;
    public const bool AllowsProductionWrite = GreeAliceRegistryImportBoundary.AllowsProductionWrite;
    public const bool AllowsDeviceControl = GreeAliceRegistryImportBoundary.AllowsDeviceControl;
    public const bool AllowsMqtt = GreeAliceRegistryImportBoundary.AllowsMqtt;
    public const bool ProductionWiringAllowed = GreeAliceRegistryImportBoundary.ProductionWiringAllowed;
}
