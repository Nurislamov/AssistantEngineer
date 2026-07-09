namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

public static class GreeCloudControlSafetyLimit
{
    public const int MinTargetTemperatureC = 18;
    public const int MaxTargetTemperatureC = 30;
    public const bool RequiresSingleDeviceScope = true;
    public const bool RequiresRateLimit = true;
    public const bool RequiresAuditEvent = true;
}
