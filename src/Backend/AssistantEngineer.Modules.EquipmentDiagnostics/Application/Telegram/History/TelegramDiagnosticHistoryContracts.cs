using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;

public enum TelegramDiagnosticCaseSource
{
    Telegram
}

public enum TelegramDiagnosticCaseStatus
{
    Completed,
    NotFound
}

public enum TelegramDiagnosticCaseResponseMode
{
    Consumer,
    Technical
}

public sealed class TelegramDiagnosticCaseEntity
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public long? TelegramConversationSessionId { get; set; }
    public TelegramDiagnosticCaseSource Source { get; set; } = TelegramDiagnosticCaseSource.Telegram;
    public TelegramDiagnosticCaseStatus Status { get; set; }
    public TelegramUserRole UserRoleAtCreation { get; set; }
    public TelegramDiagnosticCaseResponseMode ResponseMode { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? EquipmentType { get; set; }
    public string? DisplayContext { get; set; }
    public string? ResultSummary { get; set; }
    public string? NormalizedRequestJson { get; set; }
    public int? CandidateCount { get; set; }
    public bool PhoneWasSaved { get; set; }
    public TelegramUserPhoneNumberSource? PhoneNumberSource { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed record TelegramDiagnosticCaseSnapshot(
    long Id,
    long TelegramUserId,
    long? TelegramConversationSessionId,
    TelegramDiagnosticCaseSource Source,
    TelegramDiagnosticCaseStatus Status,
    TelegramUserRole UserRoleAtCreation,
    TelegramDiagnosticCaseResponseMode ResponseMode,
    string Code,
    string? Manufacturer,
    string? EquipmentType,
    string? DisplayContext,
    string? ResultSummary,
    string? NormalizedRequestJson,
    int? CandidateCount,
    bool PhoneWasSaved,
    TelegramUserPhoneNumberSource? PhoneNumberSource,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record TelegramDiagnosticCaseCreate(
    long TelegramUserId,
    long? TelegramConversationSessionId,
    TelegramDiagnosticCaseStatus Status,
    TelegramUserRole UserRoleAtCreation,
    TelegramDiagnosticCaseResponseMode ResponseMode,
    string Code,
    string? Manufacturer,
    string? EquipmentType,
    string? DisplayContext,
    string? ResultSummary,
    string? NormalizedRequestJson,
    int? CandidateCount,
    bool PhoneWasSaved,
    TelegramUserPhoneNumberSource? PhoneNumberSource,
    DateTimeOffset CreatedAt);

public interface ITelegramDiagnosticCaseStore
{
    Task<TelegramDiagnosticCaseSnapshot> CreateAsync(
        TelegramDiagnosticCaseCreate diagnosticCase,
        CancellationToken cancellationToken = default);

    Task<TelegramDiagnosticCaseSnapshot?> GetLastForTelegramUserAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramDiagnosticCaseSnapshot>> GetLatestForTelegramUserAsync(
        long telegramUserId,
        int limit,
        CancellationToken cancellationToken = default);
}
