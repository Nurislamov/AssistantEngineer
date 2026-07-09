namespace AssistantEngineer.GreeAliceBridge.Contracts.Pilot;

public static class GreeAliceMinimalProductionPilotRequirement
{
    public const string RepositoryCleanAndSynced = nameof(RepositoryCleanAndSynced);
    public const string AllTestsPass = nameof(AllTestsPass);
    public const string BridgeRemainsIsolated = nameof(BridgeRemainsIsolated);
    public const string ProductionWiringReviewedAndDisabled = nameof(ProductionWiringReviewedAndDisabled);
    public const string LiveReadOnlyPilotApprovedSeparately = nameof(LiveReadOnlyPilotApprovedSeparately);
    public const string ControlApprovalRemainsSeparate = nameof(ControlApprovalRemainsSeparate);
    public const string MqttBlocked = nameof(MqttBlocked);
    public const string NoSecretsInRepository = nameof(NoSecretsInRepository);
    public const string ExternalAuthMaterialOutsideRepository = nameof(ExternalAuthMaterialOutsideRepository);
    public const string EvidenceMasksAccountAndDeviceIdentifiers = nameof(EvidenceMasksAccountAndDeviceIdentifiers);
    public const string ExactOperatorApproved = nameof(ExactOperatorApproved);
    public const string ExactAccountScopeApproved = nameof(ExactAccountScopeApproved);
    public const string ExactHomeScopeApproved = nameof(ExactHomeScopeApproved);
    public const string ExactDeviceOrChildUnitScopeApproved = nameof(ExactDeviceOrChildUnitScopeApproved);
    public const string ReadOnlyFirstAccepted = nameof(ReadOnlyFirstAccepted);
    public const string AuditEventFormatApproved = nameof(AuditEventFormatApproved);
    public const string MonitoringPlanDocumented = nameof(MonitoringPlanDocumented);
    public const string KillSwitchPlanDocumented = nameof(KillSwitchPlanDocumented);
    public const string RollbackPlanDocumented = nameof(RollbackPlanDocumented);
    public const string ManualApprovalRecorded = nameof(ManualApprovalRecorded);
}
