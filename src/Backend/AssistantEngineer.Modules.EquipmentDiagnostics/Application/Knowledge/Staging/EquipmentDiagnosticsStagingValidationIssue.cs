namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;

public enum EquipmentDiagnosticsStagingValidationIssueSeverity
{
    Info,
    Warning,
    Error
}

public sealed record EquipmentDiagnosticsStagingValidationIssue(
    string Code,
    string Path,
    string Message,
    EquipmentDiagnosticsStagingValidationIssueSeverity Severity);
