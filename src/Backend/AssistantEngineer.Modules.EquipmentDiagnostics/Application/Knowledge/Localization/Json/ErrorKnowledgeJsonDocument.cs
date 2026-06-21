namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;

public sealed class ErrorKnowledgeJsonDocument
{
    public string? Id { get; set; }

    public string? Manufacturer { get; set; }

    public string? EquipmentFamily { get; set; }

    public string? EquipmentType { get; set; }

    public string? Series { get; set; }

    public List<string>? Models { get; set; }

    public string? Code { get; set; }

    public string? SignalType { get; set; }

    public string? DisplaySource { get; set; }

    public string? SystemPart { get; set; }

    public string? Severity { get; set; }

    public bool? RequiresQualifiedService { get; set; }

    public bool? CanCustomerContinueOperation { get; set; }

    public string? PackageId { get; set; }

    public string? SourceLanguage { get; set; }

    public string? SourceType { get; set; }

    public string? SourceName { get; set; }

    public string? SourceMeaning { get; set; }

    public string? SourceReference { get; set; }

    public string? Confidence { get; set; }

    public string? VerificationStatus { get; set; }

    public List<ErrorKnowledgeJsonSourceReference>? SourceReferences { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public List<ErrorKnowledgeJsonText>? Texts { get; set; }
}

public sealed class ErrorKnowledgeJsonSourceReference
{
    public string? SourceName { get; set; }

    public string? DocumentCode { get; set; }

    public string? SourceReference { get; set; }

    public string? SourceType { get; set; }

    public string? SourceLanguage { get; set; }

    public string? VerificationStatus { get; set; }

    public string? Confidence { get; set; }

    public string? ManualId { get; set; }

    public string? PackageId { get; set; }

    public string? Notes { get; set; }
}

public sealed class ErrorKnowledgePackageJsonDocument
{
    public string? PackageId { get; set; }

    public string? Manufacturer { get; set; }

    public string? EquipmentFamily { get; set; }

    public string? Series { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? SourceLanguage { get; set; }

    public string? SourceType { get; set; }

    public string? SourceName { get; set; }

    public string? SourceReference { get; set; }

    public string? VerificationStatus { get; set; }

    public string? Confidence { get; set; }

    public List<string>? IntendedSignalTypes { get; set; }

    public List<string>? IntendedEquipmentTypes { get; set; }

    public List<string>? IntendedDisplaySources { get; set; }

    public int? EntryCountExpected { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ErrorKnowledgeJsonText
{
    public string? Id { get; set; }

    public string? Locale { get; set; }

    public string? Audience { get; set; }

    public string? Title { get; set; }

    public string? Summary { get; set; }

    public string? SafetyNote { get; set; }

    public List<string>? PossibleCauses { get; set; }

    public List<string>? CheckSteps { get; set; }

    public List<string>? DoNotAdvise { get; set; }

    public string? RecommendedAction { get; set; }

    public string? SourceNote { get; set; }

    public bool IsMachineTranslated { get; set; }

    public bool IsReviewed { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
