namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

public static class GreeCloudLiveReadOnlyPilotGateBoundary
{
    public const string PilotGateStatus = GreeCloudLiveReadOnlyPilotGateStatus.NotApproved;
    public const bool PilotGateOpen = false;
    public const bool LiveReadOnlyPilotAllowed = false;
    public const bool LiveReadOnlyAdapterEnabled = false;
    public const bool LiveControlAllowed = false;
    public const bool MqttAllowed = false;
    public const bool ProductionWiringAllowed = false;
    public const bool RequiresManualApproval = true;
    public const bool RequiresMaskedEvidence = true;
    public const bool RequiresExternalSecretStore = true;
    public const bool RequiresRollbackPlan = true;
    public const bool RequiresKillSwitchPlan = true;
    public const bool RequiresOperatorNamedDeviceScope = true;

    public static IReadOnlyList<string> RequiredRequirements { get; } =
    [
        GreeCloudLiveReadOnlyPilotGateRequirement.RepositoryCleanAndSynced,
        GreeCloudLiveReadOnlyPilotGateRequirement.AllTestsPass,
        GreeCloudLiveReadOnlyPilotGateRequirement.BridgeRemainsIsolated,
        GreeCloudLiveReadOnlyPilotGateRequirement.ControlAdapterBlocked,
        GreeCloudLiveReadOnlyPilotGateRequirement.MqttBlocked,
        GreeCloudLiveReadOnlyPilotGateRequirement.NoProductionDeploymentWiring,
        GreeCloudLiveReadOnlyPilotGateRequirement.NoSecretsInRepository,
        GreeCloudLiveReadOnlyPilotGateRequirement.CredentialsStoredOutsideRepository,
        GreeCloudLiveReadOnlyPilotGateRequirement.EvidenceMasksAccountAndDeviceIdentifiers,
        GreeCloudLiveReadOnlyPilotGateRequirement.OperatorApprovesExactAccountAndDeviceScope,
        GreeCloudLiveReadOnlyPilotGateRequirement.KillSwitchPlanDocumented,
        GreeCloudLiveReadOnlyPilotGateRequirement.RollbackPlanDocumented,
        GreeCloudLiveReadOnlyPilotGateRequirement.PilotLimitedToReadOnly,
        GreeCloudLiveReadOnlyPilotGateRequirement.ReadOnlyAdapterImplementationReviewed,
        GreeCloudLiveReadOnlyPilotGateRequirement.ManualApprovalRecorded
    ];
}
