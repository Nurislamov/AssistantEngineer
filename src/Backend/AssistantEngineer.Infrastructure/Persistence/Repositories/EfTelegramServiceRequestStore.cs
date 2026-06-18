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

    public async Task<TelegramServiceRequestSnapshot?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var request = await context.TelegramServiceRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return request is null ? null : ToSnapshot(request);
    }

    public async Task<IReadOnlyList<TelegramServiceRequestSnapshot>> GetActiveAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var requests = await context.TelegramServiceRequests
            .AsNoTracking()
            .Where(item => item.Status == TelegramServiceRequestStatus.New ||
                item.Status == TelegramServiceRequestStatus.InProgress)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArrayAsync(cancellationToken);
        return requests.Select(ToSnapshot).ToArray();
    }

    public async Task<TelegramServiceRequestSnapshot?> UpdateAsync(
        TelegramServiceRequestUpdate update,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await context.TelegramServiceRequests
            .FirstOrDefaultAsync(item => item.Id == update.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Status = update.Status;
        entity.AssignedTelegramUserId = update.AssignedTelegramUserId;
        entity.AssignedAt = update.AssignedAt;
        entity.AssignedByTelegramUserId = update.AssignedByTelegramUserId;
        entity.StatusUpdatedAt = update.StatusUpdatedAt;
        entity.StatusUpdatedByTelegramUserId = update.StatusUpdatedByTelegramUserId;
        entity.UpdatedAt = update.StatusUpdatedAt;
        entity.ClosedAt = update.ClosedAt;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task<TelegramServiceRequestSnapshot?> UpdateNotificationAsync(
        TelegramServiceRequestNotificationUpdate update,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await context.TelegramServiceRequests
            .FirstOrDefaultAsync(item => item.Id == update.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.NotificationChatId = update.NotificationChatId;
        entity.NotificationMessageId = update.NotificationMessageId;
        entity.NotificationSentAt = update.NotificationSentAt;
        entity.NotificationUpdatedAt = update.NotificationUpdatedAt;
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
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
            entity.AssignedTelegramUserId,
            entity.AssignedAt,
            entity.AssignedByTelegramUserId,
            entity.StatusUpdatedAt,
            entity.StatusUpdatedByTelegramUserId,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.ClosedAt,
            entity.NotificationChatId,
            entity.NotificationMessageId,
            entity.NotificationSentAt,
            entity.NotificationUpdatedAt);
}
