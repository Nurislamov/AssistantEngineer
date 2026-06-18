using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public enum TelegramServiceRequestSource
{
    Telegram
}

public enum TelegramServiceRequestStatus
{
    New,
    InProgress,
    Resolved,
    Cancelled
}

public sealed class TelegramServiceRequestEntity
{
    public long Id { get; set; }
    public long TelegramUserId { get; set; }
    public long DiagnosticCaseId { get; set; }
    public TelegramServiceRequestSource Source { get; set; } = TelegramServiceRequestSource.Telegram;
    public TelegramServiceRequestStatus Status { get; set; } = TelegramServiceRequestStatus.New;
    public string Code { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? EquipmentType { get; set; }
    public string? DisplayContext { get; set; }
    public bool PhoneWasSaved { get; set; }
    public TelegramUserPhoneNumberSource? PhoneNumberSource { get; set; }
    public string? ContactPhoneLast4 { get; set; }
    public TelegramUserRole UserRoleAtCreation { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed record TelegramServiceRequestSnapshot(
    long Id,
    long TelegramUserId,
    long DiagnosticCaseId,
    TelegramServiceRequestSource Source,
    TelegramServiceRequestStatus Status,
    string Code,
    string? Manufacturer,
    string? EquipmentType,
    string? DisplayContext,
    bool PhoneWasSaved,
    TelegramUserPhoneNumberSource? PhoneNumberSource,
    string? ContactPhoneLast4,
    TelegramUserRole UserRoleAtCreation,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ClosedAt);

public sealed record TelegramServiceRequestCreate(
    long TelegramUserId,
    long DiagnosticCaseId,
    string Code,
    string? Manufacturer,
    string? EquipmentType,
    string? DisplayContext,
    bool PhoneWasSaved,
    TelegramUserPhoneNumberSource? PhoneNumberSource,
    TelegramUserRole UserRoleAtCreation,
    DateTimeOffset CreatedAt);

public sealed record TelegramServiceRequestCreateResult(
    TelegramServiceRequestSnapshot Request,
    bool Created);

public interface ITelegramServiceRequestStore
{
    Task<TelegramServiceRequestCreateResult> CreateIfNoActiveAsync(
        TelegramServiceRequestCreate request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestForTelegramUserAsync(
        long telegramUserId,
        int limit,
        CancellationToken cancellationToken = default);
}

public enum TelegramServiceRequestAttemptStatus
{
    Created,
    Existing,
    NoDiagnosticCase,
    PhoneMissing
}

public sealed record TelegramServiceRequestAttemptResult(
    TelegramServiceRequestAttemptStatus Status,
    TelegramServiceRequestSnapshot? Request,
    string Text);
