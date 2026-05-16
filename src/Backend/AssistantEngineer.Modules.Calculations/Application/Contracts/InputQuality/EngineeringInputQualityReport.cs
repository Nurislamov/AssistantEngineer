namespace AssistantEngineer.Modules.Calculations.Application.Contracts.InputQuality;

public sealed record EngineeringInputQualityReport(
    string Scope,
    string SubjectType,
    int? SubjectId,
    IReadOnlyList<EngineeringInputQualityDiagnostic> Diagnostics,
    EngineeringInputQualitySeverity HighestSeverity,
    bool HasBlockingIssues,
    bool HasWarnings,
    bool IsCalculationReady);
