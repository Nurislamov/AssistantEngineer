namespace AssistantEngineer.SharedKernel.Diagnostics;

public static class ObservabilityEventCodes
{
    public const string WorkflowStateLoaded = "OBS-WF-001";
    public const string WorkflowScenarioExecutionStarted = "OBS-WF-002";
    public const string WorkflowScenarioExecutionCompleted = "OBS-WF-003";
    public const string WorkflowScenarioExecutionFailed = "OBS-WF-004";

    public const string JobClaimAttempted = "OBS-JOB-001";
    public const string JobClaimSucceeded = "OBS-JOB-002";
    public const string JobClaimSkipped = "OBS-JOB-003";
    public const string JobExecutionStarted = "OBS-JOB-004";
    public const string JobExecutionCompleted = "OBS-JOB-005";
    public const string JobExecutionFailed = "OBS-JOB-006";

    public const string CalculationStarted = "OBS-CALC-001";
    public const string CalculationCompleted = "OBS-CALC-002";
    public const string CalculationValidationFailed = "OBS-CALC-003";
    public const string CalculationDiagnosticsEmitted = "OBS-CALC-004";

    public const string InputQualityCheckStarted = "OBS-IQ-001";
    public const string InputQualityCheckCompleted = "OBS-IQ-002";
    public const string InputQualityBlockingIssueDetected = "OBS-IQ-003";

    public const string ArtifactWriteStarted = "OBS-ART-001";
    public const string ArtifactWriteCompleted = "OBS-ART-002";
    public const string ArtifactReadCompleted = "OBS-ART-003";
    public const string ArtifactIntegrityCheckFailed = "OBS-ART-004";
    public const string ArtifactSizeLimitExceeded = "OBS-ART-005";

    public const string PersistenceOperationStarted = "OBS-PER-001";
    public const string PersistenceOperationCompleted = "OBS-PER-002";
    public const string PersistenceConflictDetected = "OBS-PER-003";
    public const string PersistenceOperationFailed = "OBS-PER-004";

    public const string ValidationFixtureChecked = "OBS-VAL-001";
    public const string ValidationToleranceComparisonFailed = "OBS-VAL-002";

    public const string GovernanceGuardExecuted = "OBS-GOV-001";
    public const string GovernanceGuardFailed = "OBS-GOV-002";
}
