namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

public sealed record ErrorKnowledgeJsonSource(string Path, string Json);

public sealed record ErrorKnowledgeValidationIssue(string Path, string Problem);

public sealed record ErrorKnowledgePackageManifest(
    string PackageId,
    string Manufacturer,
    ErrorKnowledgeEquipmentFamily EquipmentFamily,
    string? Series,
    string Title,
    string Description,
    string SourceLanguage,
    string SourceType,
    string SourceName,
    string? SourceReference,
    string VerificationStatus,
    string Confidence,
    IReadOnlyList<ErrorKnowledgeSignalType> IntendedSignalTypes,
    IReadOnlyList<ErrorKnowledgeEquipmentType> IntendedEquipmentTypes,
    IReadOnlyList<ErrorKnowledgeDisplaySource> IntendedDisplaySources,
    int? EntryCountExpected,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string SourcePath);

public sealed record ErrorKnowledgeValidationResult(
    IReadOnlyList<ErrorKnowledgePackageManifest> Packages,
    IReadOnlyList<ErrorKnowledgeEntryV2> Entries,
    IReadOnlyList<ErrorKnowledgeValidationIssue> Issues)
{
    public bool IsValid => Issues.Count == 0;
}
