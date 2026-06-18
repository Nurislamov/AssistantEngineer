using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramServiceRequestStore : ITelegramServiceRequestStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramServiceRequestStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramServiceRequestCreateResult> CreateIfNoActiveAsync(
        TelegramServiceRequestCreate request,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await ActiveQuery(context, request.DiagnosticCaseId)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return new TelegramServiceRequestCreateResult(ToSnapshot(existing), false);
        }

        var entity = new TelegramServiceRequestEntity
        {
            TelegramUserId = request.TelegramUserId,
            DiagnosticCaseId = request.DiagnosticCaseId,
            Status = TelegramServiceRequestStatus.New,
            Code = request.Code,
            Manufacturer = request.Manufacturer,
            EquipmentType = request.EquipmentType,
            DisplayContext = request.DisplayContext,
            PhoneWasSaved = request.PhoneWasSaved,
            PhoneNumberSource = request.PhoneNumberSource,
            UserRoleAtCreation = request.UserRoleAtCreation,
            CreatedAt = request.CreatedAt
        };

        await context.TelegramServiceRequests.AddAsync(entity, cancellationToken);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return new TelegramServiceRequestCreateResult(ToSnapshot(entity), true);
        }
        catch (DbUpdateException)
        {
            context.Entry(entity).State = EntityState.Detached;
            existing = await ActiveQuery(context, request.DiagnosticCaseId)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null)
            {
                throw;
            }

            return new TelegramServiceRequestCreateResult(ToSnapshot(existing), false);
        }
    }

    public async Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetLatestForTelegramUserAsync(
        long telegramUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var requests = await context.TelegramServiceRequests
            .AsNoTracking()
            .Where(item => item.TelegramUserId == telegramUserId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 20))
            .ToArrayAsync(cancellationToken);
        return requests.Select(ToSnapshot).ToArray();
    }

    private static IQueryable<TelegramServiceRequestEntity> ActiveQuery(
        AppDbContext context,
        long diagnosticCaseId) =>
        context.TelegramServiceRequests
            .AsNoTracking()
            .Where(item => item.DiagnosticCaseId == diagnosticCaseId)
            .Where(item => item.Status == TelegramServiceRequestStatus.New ||
                item.Status == TelegramServiceRequestStatus.InProgress)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id);

    private static TelegramServiceRequestSnapshot ToSnapshot(TelegramServiceRequestEntity entity) =>
        new(
            entity.Id,
            entity.TelegramUserId,
            entity.DiagnosticCaseId,
            entity.Source,
            entity.Status,
            entity.Code,
            entity.Manufacturer,
            entity.EquipmentType,
            entity.DisplayContext,
            entity.PhoneWasSaved,
            entity.PhoneNumberSource,
            entity.ContactPhoneLast4,
            entity.UserRoleAtCreation,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.ClosedAt);
}
