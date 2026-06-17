using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramDiagnosticCaseStore : ITelegramDiagnosticCaseStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramDiagnosticCaseStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramDiagnosticCaseSnapshot> CreateAsync(
        TelegramDiagnosticCaseCreate diagnosticCase,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new TelegramDiagnosticCaseEntity
        {
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

        await context.TelegramDiagnosticCases.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task<TelegramDiagnosticCaseSnapshot?> GetLastForTelegramUserAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var diagnosticCase = await context.TelegramDiagnosticCases
            .AsNoTracking()
            .Where(item => item.TelegramUserId == telegramUserId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return diagnosticCase is null ? null : ToSnapshot(diagnosticCase);
    }

    public async Task<IReadOnlyList<TelegramDiagnosticCaseSnapshot>> GetLatestForTelegramUserAsync(
        long telegramUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cases = await context.TelegramDiagnosticCases
            .AsNoTracking()
            .Where(item => item.TelegramUserId == telegramUserId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 20))
            .ToArrayAsync(cancellationToken);

        return cases.Select(ToSnapshot).ToArray();
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
