namespace AssistantEngineer.GreeAliceBridge.Contracts.Pilot;

public static class GreeAliceMinimalProductionPilotBoundary
{
    public const string ProductionPilotStatus = GreeAliceMinimalProductionPilotStatus.NotApproved;
    public const bool ProductionPilotApproved = false;
    public const bool ProductionPilotEnabled = false;
    public const bool ProductionDeploymentWiringEnabled = false;
    public const bool LiveGreeRuntimeEnabled = false;
    public const bool LiveReadOnlyPilotEnabled = false;
    public const bool LiveControlEnabled = false;
    public const bool MqttAllowed = false;
    public const bool SecretsInRepositoryAllowed = false;
    public const bool ReadOnlyFirstRequired = true;
    public const bool RequiresSingleOperatorScope = true;
    public const bool RequiresSingleAccountScope = true;
    public const bool RequiresSingleHomeScope = true;
    public const bool RequiresSingleDeviceOrChildUnitScope = true;
    public const bool RequiresManualApproval = true;
    public const bool RequiresAuditLogging = true;
    public const bool RequiresMonitoringPlan = true;
    public const bool RequiresKillSwitchPlan = true;
    public const bool RequiresRollbackPlan = true;
    public const string DefaultMode = GreeAliceMinimalProductionPilotMode.Blocked;

    public static IReadOnlyList<string> PilotModes { get; } =
    [
        GreeAliceMinimalProductionPilotMode.ReadOnlyFirst,
        GreeAliceMinimalProductionPilotMode.SingleDeviceReadOnly,
        GreeAliceMinimalProductionPilotMode.SingleVrfChildReadOnly,
        GreeAliceMinimalProductionPilotMode.SingleDeviceControlCandidate,
        GreeAliceMinimalProductionPilotMode.Blocked
    ];

    public static GreeAliceMinimalProductionPilotScope DefaultScope { get; } = new(
        OperatorId: "dummy-operator-001",
        AccountScope: "dummy-account-001",
        HomeScope: "dummy-home-001",
        DeviceScope: "dummy-gree-ac-001",
        VrfChildUnitScope: "dummy-vrf-child-001",
        Mode: DefaultMode,
        IsMasked: true,
        IsDummyOrTemplate: true);

    public static IReadOnlyList<string> RequiredRequirements { get; } =
    [
        GreeAliceMinimalProductionPilotRequirement.RepositoryCleanAndSynced,
        GreeAliceMinimalProductionPilotRequirement.AllTestsPass,
        GreeAliceMinimalProductionPilotRequirement.BridgeRemainsIsolated,
        GreeAliceMinimalProductionPilotRequirement.ProductionWiringReviewedAndDisabled,
        GreeAliceMinimalProductionPilotRequirement.LiveReadOnlyPilotApprovedSeparately,
        GreeAliceMinimalProductionPilotRequirement.ControlApprovalRemainsSeparate,
        GreeAliceMinimalProductionPilotRequirement.MqttBlocked,
        GreeAliceMinimalProductionPilotRequirement.NoSecretsInRepository,
        GreeAliceMinimalProductionPilotRequirement.ExternalAuthMaterialOutsideRepository,
        GreeAliceMinimalProductionPilotRequirement.EvidenceMasksAccountAndDeviceIdentifiers,
        GreeAliceMinimalProductionPilotRequirement.ExactOperatorApproved,
        GreeAliceMinimalProductionPilotRequirement.ExactAccountScopeApproved,
        GreeAliceMinimalProductionPilotRequirement.ExactHomeScopeApproved,
        GreeAliceMinimalProductionPilotRequirement.ExactDeviceOrChildUnitScopeApproved,
        GreeAliceMinimalProductionPilotRequirement.ReadOnlyFirstAccepted,
        GreeAliceMinimalProductionPilotRequirement.AuditEventFormatApproved,
        GreeAliceMinimalProductionPilotRequirement.MonitoringPlanDocumented,
        GreeAliceMinimalProductionPilotRequirement.KillSwitchPlanDocumented,
        GreeAliceMinimalProductionPilotRequirement.RollbackPlanDocumented,
        GreeAliceMinimalProductionPilotRequirement.ManualApprovalRecorded
    ];
}
