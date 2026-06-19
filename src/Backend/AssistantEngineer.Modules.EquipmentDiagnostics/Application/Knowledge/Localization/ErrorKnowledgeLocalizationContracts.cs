using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;

public enum ErrorKnowledgeAudience
{
    Consumer,
    Installer,
    Engineer
}

public sealed record ErrorKnowledgeEntryV2(
    string Id,
    string Manufacturer,
    string? EquipmentType,
    string? Series,
    IReadOnlyList<string> Models,
    string Code,
    string SourceLanguage,
    string SourceType,
    string SourceName,
    string? SourceReference,
    string Confidence,
    string VerificationStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ErrorKnowledgeTextV2> Texts);

public sealed record ErrorKnowledgeTextV2(
    string Id,
    string EntryId,
    string Locale,
    ErrorKnowledgeAudience Audience,
    string Title,
    string Summary,
    string SafetyNote,
    IReadOnlyList<string> PossibleCauses,
    IReadOnlyList<string> CheckSteps,
    IReadOnlyList<string> DoNotAdvise,
    string RecommendedAction,
    string SourceNote,
    bool IsMachineTranslated,
    bool IsReviewed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ErrorKnowledgeLocalizationSelection(
    ErrorKnowledgeEntryV2 Entry,
    ErrorKnowledgeTextV2 Text);

public interface IErrorKnowledgeLocalizationSource
{
    IReadOnlyCollection<ErrorKnowledgeEntryV2> GetEntries();

    ErrorKnowledgeLocalizationSelection? Select(
        EquipmentDiagnosticBotResponse response,
        string locale,
        ErrorKnowledgeAudience audience);
}
