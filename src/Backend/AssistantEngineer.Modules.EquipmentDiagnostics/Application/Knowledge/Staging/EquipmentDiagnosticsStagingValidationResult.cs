namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;

public sealed record EquipmentDiagnosticsStagingValidationResult(
    IReadOnlyList<EquipmentDiagnosticsStagingValidationIssue> Issues)
{
    public bool IsValid =>
        Issues.All(issue => issue.Severity != EquipmentDiagnosticsStagingValidationIssueSeverity.Error);

    public IReadOnlyList<EquipmentDiagnosticsStagingValidationIssue> Errors =>
        Issues
            .Where(issue => issue.Severity == EquipmentDiagnosticsStagingValidationIssueSeverity.Error)
            .ToArray();
}
