namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlPilot;

public static class GreeCloudSingleDeviceControlPilotBoundary
{
    public const string PilotStatus = GreeCloudSingleDeviceControlPilotStatus.NotApproved;
    public const bool SingleDevicePilotImplemented = true;
    public const bool SingleDevicePilotApproved = false;
    public const bool LiveControlEnabled = false;
    public const bool DryRunOnly = true;
    public const bool FailClosed = true;
    public const bool CommandSendingEnabled = false;
    public const bool MqttAllowed = false;
    public const bool ProductionWiringAllowed = false;
    public const bool RequiresExactSingleDeviceScope = true;
    public const bool RequiresManualApproval = true;
    public const bool RequiresAuditEvent = true;
    public const bool RequiresKillSwitch = true;
    public const bool RequiresRollbackPlan = true;

    public const string PilotAccountId = "dummy-account-001";
    public const string PilotDeviceId = "dummy-gree-ac-001";
    public const string PilotDeviceKind = "split-ac";
    public const string PilotScopeKind = "single-device-offline-fixture";

    public const int MinTargetTemperatureC = 18;
    public const int MaxTargetTemperatureC = 30;
    public const bool RateLimitRequired = true;
    public const bool SingleDeviceScopeRequired = true;
    public const bool AuditEventRequired = true;
    public const bool KillSwitchRequired = true;
    public const bool RollbackRequired = true;

    public static IReadOnlyList<string> AllowedModesCandidate { get; } =
    [
        "auto",
        "cool",
        "heat",
        "dry",
        "fan"
    ];

    public static IReadOnlyList<string> AllowedFanSpeedsCandidate { get; } =
    [
        "auto",
        "low",
        "medium",
        "high"
    ];

    public static GreeCloudSingleDeviceControlPilotScope DummyScope { get; } = new(
        PilotAccountId,
        PilotDeviceId,
        PilotDeviceKind,
        PilotScopeKind);

    public static IReadOnlyList<GreeCloudSingleDeviceControlPilotCommand> CandidateCommandPlan { get; } =
    [
        CreateCandidate("power_on_off"),
        CreateCandidate("set_mode"),
        CreateCandidate("set_target_temperature"),
        CreateCandidate("set_fan_speed"),
        CreateCandidate("set_swing_vertical"),
        CreateCandidate("set_swing_horizontal")
    ];

    private static GreeCloudSingleDeviceControlPilotCommand CreateCandidate(string name)
    {
        return new GreeCloudSingleDeviceControlPilotCommand(
            name,
            IsApproved: false,
            DryRunOnly,
            WillSendToGreeCloud: false,
            WillSendToMqtt: false,
            WillSendDeviceCommand: false,
            RequiresManualApproval,
            RequiresAuditEvent,
            RequiresKillSwitchClear: true);
    }
}
