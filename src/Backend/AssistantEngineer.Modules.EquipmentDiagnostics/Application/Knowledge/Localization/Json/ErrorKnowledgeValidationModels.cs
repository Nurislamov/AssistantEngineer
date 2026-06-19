namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

public sealed record ErrorKnowledgeJsonSource(string Path, string Json);

public sealed record ErrorKnowledgeValidationIssue(string Path, string Problem);

public sealed record ErrorKnowledgeValidationResult(
    IReadOnlyList<ErrorKnowledgeEntryV2> Entries,
    IReadOnlyList<ErrorKnowledgeValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}
