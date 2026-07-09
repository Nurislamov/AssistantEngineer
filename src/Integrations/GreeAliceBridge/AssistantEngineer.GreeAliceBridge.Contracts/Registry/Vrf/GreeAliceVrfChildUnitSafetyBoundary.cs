namespace AssistantEngineer.GreeAliceBridge.Contracts.Registry.Vrf;

public static class GreeAliceVrfChildUnitSafetyBoundary
{
    public const string RuntimeMode = "offline-fixture";
    public const bool GatewayExposedByDefault = false;
    public const bool ChildExposureRequiresExplicitFlag = true;
    public const bool UsesLiveGreeCloud = false;
    public const bool UsesMqtt = false;
    public const bool AllowsDeviceControl = false;
    public const bool AllowsProductionWiring = false;
}
