namespace AssistantEngineer.GreeAliceBridge.Contracts.Safety;

public static class GreeAliceBridgeSafetyMiddlewareBoundary
{
    public const string BoundaryMode = "offline-safety-filter";
    public const bool AppliesToIsolatedBridgeApiOnly = true;
    public const bool AllowsProductionRuntimeWiring = false;
    public const bool AllowsLiveControl = false;
}
