namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Governance;

public sealed record EngineeringGovernanceCheckDiagnostic(
    string Code,
    EngineeringGovernanceDiagnosticSeverity Severity,
    string Message,
    string? FilePath = null,
    int? LineNumber = null,
    string? StageId = null,
    string? Token = null);
