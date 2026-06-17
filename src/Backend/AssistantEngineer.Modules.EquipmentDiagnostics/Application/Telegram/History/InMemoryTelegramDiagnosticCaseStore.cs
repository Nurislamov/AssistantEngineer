using System.Collections.Concurrent;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;

public sealed class InMemoryTelegramDiagnosticCaseStore : ITelegramDiagnosticCaseStore
{
    private readonly ConcurrentDictionary<long, TelegramDiagnosticCaseEntity> _cases = new();
    private long _nextId;

    public Task<TelegramDiagnosticCaseSnapshot> CreateAsync(
        TelegramDiagnosticCaseCreate diagnosticCase,
        CancellationToken cancellationToken = default)
    {
        var entity = new TelegramDiagnosticCaseEntity
        {
            Id = Interlocked.Increment(ref _nextId),
            TelegramUserId = diagnosticCase.TelegramUserId,
            TelegramConversationSessionId = diagnosticCase.TelegramConversationSessionId,
            Status = diagnosticCase.Status,
            UserRoleAtCreation = diagnosticCase.UserRoleAtCreation,
            ResponseMode = diagnosticCase.ResponseMode,
            Code = diagnosticCase.Code,
            Manufacturer = diagnosticCase.Manufacturer,
            EquipmentType = diagnosticCase.EquipmentType,
            DisplayContext = diagnosticCase.DisplayContext,
            ResultSummary = diagnosticCase.ResultSummary,
            NormalizedRequestJson = diagnosticCase.NormalizedRequestJson,
            CandidateCount = diagnosticCase.CandidateCount,
            PhoneWasSaved = diagnosticCase.PhoneWasSaved,
            PhoneNumberSource = diagnosticCase.PhoneNumberSource,
            CreatedAt = diagnosticCase.CreatedAt
        };

        _cases[entity.Id] = entity;
        return Task.FromResult(ToSnapshot(entity));
    }

    public Task<TelegramDiagnosticCaseSnapshot?> GetLastForTelegramUserAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var diagnosticCase = _cases.Values
            .Where(item => item.TelegramUserId == telegramUserId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();

        return Task.FromResult(diagnosticCase is null ? null : ToSnapshot(diagnosticCase));
    }

    public Task<IReadOnlyList<TelegramDiagnosticCaseSnapshot>> GetLatestForTelegramUserAsync(
        long telegramUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var cases = _cases.Values
            .Where(item => item.TelegramUserId == telegramUserId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 20))
            .Select(ToSnapshot)
            .ToArray();

        return Task.FromResult<IReadOnlyList<TelegramDiagnosticCaseSnapshot>>(cases);
    }

    private static TelegramDiagnosticCaseSnapshot ToSnapshot(TelegramDiagnosticCaseEntity entity) =>
        new(
            entity.Id,
            entity.TelegramUserId,
            entity.TelegramConversationSessionId,
            entity.Source,
            entity.Status,
            entity.UserRoleAtCreation,
            entity.ResponseMode,
            entity.Code,
            entity.Manufacturer,
            entity.EquipmentType,
            entity.DisplayContext,
            entity.ResultSummary,
            entity.NormalizedRequestJson,
            entity.CandidateCount,
            entity.PhoneWasSaved,
            entity.PhoneNumberSource,
            entity.CreatedAt,
            entity.UpdatedAt);
}
