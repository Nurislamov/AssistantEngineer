namespace AssistantEngineer.GreeAliceBridge.Contracts.GreeCloud.ControlApproval;

public static class GreeCloudControlPilotRequirement
{
    public const string RepositoryCleanAndSynced = nameof(RepositoryCleanAndSynced);
    public const string AllTestsPass = nameof(AllTestsPass);
    public const string BridgeRemainsIsolated = nameof(BridgeRemainsIsolated);
    public const string LiveReadOnlyPilotApprovedSeparately = nameof(LiveReadOnlyPilotApprovedSeparately);
    public const string ControlAdapterFailClosedUntilApproval = nameof(ControlAdapterFailClosedUntilApproval);
    public const string MqttBlocked = nameof(MqttBlocked);
    public const string NoProductionDeploymentWiring = nameof(NoProductionDeploymentWiring);
    public const string NoSecretsInRepository = nameof(NoSecretsInRepository);
    public const string ExternalAuthMaterialOutsideRepository = nameof(ExternalAuthMaterialOutsideRepository);
    public const string EvidenceMasksAccountAndDeviceIdentifiers = nameof(EvidenceMasksAccountAndDeviceIdentifiers);
    public const string ExactSingleTestAccountApproved = nameof(ExactSingleTestAccountApproved);
    public const string ExactSingleTestDeviceApproved = nameof(ExactSingleTestDeviceApproved);
    public const string ExactCommandListApproved = nameof(ExactCommandListApproved);
    public const string TemperatureLimitsApproved = nameof(TemperatureLimitsApproved);
    public const string ModeLimitsApproved = nameof(ModeLimitsApproved);
    public const string FanSwingLimitsApproved = nameof(FanSwingLimitsApproved);
    public const string RateLimitsApproved = nameof(RateLimitsApproved);
    public const string AuditEventFormatApproved = nameof(AuditEventFormatApproved);
    public const string KillSwitchPlanDocumented = nameof(KillSwitchPlanDocumented);
    public const string RollbackPlanDocumented = nameof(RollbackPlanDocumented);
    public const string ManualOperatorApprovalRecorded = nameof(ManualOperatorApprovalRecorded);
}
