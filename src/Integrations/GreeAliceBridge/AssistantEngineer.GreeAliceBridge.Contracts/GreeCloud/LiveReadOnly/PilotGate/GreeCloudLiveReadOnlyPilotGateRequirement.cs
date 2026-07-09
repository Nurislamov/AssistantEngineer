namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.LiveReadOnly.PilotGate;

public static class GreeCloudLiveReadOnlyPilotGateRequirement
{
    public const string RepositoryCleanAndSynced = nameof(RepositoryCleanAndSynced);
    public const string AllTestsPass = nameof(AllTestsPass);
    public const string BridgeRemainsIsolated = nameof(BridgeRemainsIsolated);
    public const string ControlAdapterBlocked = nameof(ControlAdapterBlocked);
    public const string MqttBlocked = nameof(MqttBlocked);
    public const string NoProductionDeploymentWiring = nameof(NoProductionDeploymentWiring);
    public const string NoSecretsInRepository = nameof(NoSecretsInRepository);
    public const string CredentialsStoredOutsideRepository = nameof(CredentialsStoredOutsideRepository);
    public const string EvidenceMasksAccountAndDeviceIdentifiers = nameof(EvidenceMasksAccountAndDeviceIdentifiers);
    public const string OperatorApprovesExactAccountAndDeviceScope = nameof(OperatorApprovesExactAccountAndDeviceScope);
    public const string KillSwitchPlanDocumented = nameof(KillSwitchPlanDocumented);
    public const string RollbackPlanDocumented = nameof(RollbackPlanDocumented);
    public const string PilotLimitedToReadOnly = nameof(PilotLimitedToReadOnly);
    public const string ReadOnlyAdapterImplementationReviewed = nameof(ReadOnlyAdapterImplementationReviewed);
    public const string ManualApprovalRecorded = nameof(ManualApprovalRecorded);
}
