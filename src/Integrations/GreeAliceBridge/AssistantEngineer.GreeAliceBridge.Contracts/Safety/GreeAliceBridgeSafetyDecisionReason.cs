namespace AssistantEngineer.GreeAliceBridge.Contracts.Safety;

public static class GreeAliceBridgeSafetyDecisionReason
{
    public const string OfflineAllowed = "offline-allowed";
    public const string DryRunFailClosed = "dry-run-fail-closed";
    public const string KillSwitchBlocked = "kill-switch-blocked";
    public const string LiveOperationBlocked = "live-operation-blocked";
    public const string ProductionWiringBlocked = "production-runtime-wiring-blocked";
}
