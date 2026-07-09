namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

public static class GreeCloudControlApprovalBoundary
{
    public const string ControlApprovalStatus = GreeCloudControlApprovalStatus.NotApproved;
    public const bool LiveControlApproved = false;
    public const bool LiveControlImplemented = false;
    public const bool LiveControlEnabled = false;
    public const bool ControlAdapterEnabled = false;
    public const bool ControlAdapterFailClosed = true;
    public const bool MqttAllowed = false;
    public const bool ProductionWiringAllowed = false;
    public const bool SingleDevicePilotApproved = false;
    public const bool RequiresManualApproval = true;
    public const bool RequiresAuditLogging = true;
    public const bool RequiresKillSwitchPlan = true;
    public const bool RequiresRollbackPlan = true;

    public static IReadOnlyList<string> CandidateOperations { get; } =
    [
        GreeCloudControlCandidateOperation.PowerOnOff,
        GreeCloudControlCandidateOperation.SetMode,
        GreeCloudControlCandidateOperation.SetTargetTemperature,
        GreeCloudControlCandidateOperation.SetFanSpeed,
        GreeCloudControlCandidateOperation.SetSwingVertical,
        GreeCloudControlCandidateOperation.SetSwingHorizontal
    ];

    public static IReadOnlyList<string> ForbiddenOperations { get; } =
    [
        GreeCloudControlForbiddenOperation.FirmwareUpdate,
        GreeCloudControlForbiddenOperation.DeviceBinding,
        GreeCloudControlForbiddenOperation.DeviceUnbinding,
        GreeCloudControlForbiddenOperation.AccountMutation,
        GreeCloudControlForbiddenOperation.ScheduleMutation,
        GreeCloudControlForbiddenOperation.TimerMutation,
        GreeCloudControlForbiddenOperation.SceneExecution,
        GreeCloudControlForbiddenOperation.BulkControl,
        GreeCloudControlForbiddenOperation.MqttConnect,
        GreeCloudControlForbiddenOperation.MqttSubscribe,
        GreeCloudControlForbiddenOperation.MqttPublish,
        GreeCloudControlForbiddenOperation.ProductionDeploy
    ];

    public static IReadOnlyList<string> RequiredRequirements { get; } =
    [
        GreeCloudControlPilotRequirement.RepositoryCleanAndSynced,
        GreeCloudControlPilotRequirement.AllTestsPass,
        GreeCloudControlPilotRequirement.BridgeRemainsIsolated,
        GreeCloudControlPilotRequirement.LiveReadOnlyPilotApprovedSeparately,
        GreeCloudControlPilotRequirement.ControlAdapterFailClosedUntilApproval,
        GreeCloudControlPilotRequirement.MqttBlocked,
        GreeCloudControlPilotRequirement.NoProductionDeploymentWiring,
        GreeCloudControlPilotRequirement.NoSecretsInRepository,
        GreeCloudControlPilotRequirement.ExternalAuthMaterialOutsideRepository,
        GreeCloudControlPilotRequirement.EvidenceMasksAccountAndDeviceIdentifiers,
        GreeCloudControlPilotRequirement.ExactSingleTestAccountApproved,
        GreeCloudControlPilotRequirement.ExactSingleTestDeviceApproved,
        GreeCloudControlPilotRequirement.ExactCommandListApproved,
        GreeCloudControlPilotRequirement.TemperatureLimitsApproved,
        GreeCloudControlPilotRequirement.ModeLimitsApproved,
        GreeCloudControlPilotRequirement.FanSwingLimitsApproved,
        GreeCloudControlPilotRequirement.RateLimitsApproved,
        GreeCloudControlPilotRequirement.AuditEventFormatApproved,
        GreeCloudControlPilotRequirement.KillSwitchPlanDocumented,
        GreeCloudControlPilotRequirement.RollbackPlanDocumented,
        GreeCloudControlPilotRequirement.ManualOperatorApprovalRecorded
    ];
}
