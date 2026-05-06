namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public sealed record EngineeringGovernanceCheckResult(
    string CheckId,
    EngineeringGovernanceReleaseReadinessStatus ReadinessStatus,
    int TotalChecks,
    int PassedChecks,
    int WarningCount,
    int ErrorCount,
    int CriticalCount,
    IReadOnlyList<EngineeringGovernanceCheckDiagnostic> Diagnostics,
    IReadOnlyList<string> StageSummaries)
{
    public bool IsSuccess => ErrorCount == 0 && CriticalCount == 0;
}
