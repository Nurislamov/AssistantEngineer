using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramServiceRequestEventStore : ITelegramServiceRequestEventStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramServiceRequestEventStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramServiceRequestEventSnapshot> AppendAsync(
        TelegramServiceRequestEventCreate request,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new TelegramServiceRequestEventEntity
        {
            ServiceRequestId = request.ServiceRequestId,
            EventType = request.EventType,
            ActorTelegramUserId = request.ActorTelegramUserId,
            TargetTelegramUserId = request.TargetTelegramUserId,
            OldStatus = request.OldStatus,
            NewStatus = request.NewStatus,
            IsSuccessful = request.IsSuccessful,
            Message = request.Message,
            MetadataJson = request.MetadataJson,
            CreatedAt = request.CreatedAt
        };
        await context.TelegramServiceRequestEvents.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(entity);
    }

    public async Task<IReadOnlyList<TelegramServiceRequestEventSnapshot>> GetLatestAsync(
        long serviceRequestId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var events = await context.TelegramServiceRequestEvents
            .AsNoTracking()
            .Where(item => item.ServiceRequestId == serviceRequestId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArrayAsync(cancellationToken);
        return events
            .Reverse()
            .Select(ToSnapshot)
            .ToArray();
    }

    private static TelegramServiceRequestEventSnapshot ToSnapshot(TelegramServiceRequestEventEntity entity) =>
        new(
            entity.Id,
            entity.ServiceRequestId,
            entity.EventType,
            entity.ActorTelegramUserId,
            entity.TargetTelegramUserId,
            entity.OldStatus,
            entity.NewStatus,
            entity.IsSuccessful,
            entity.Message,
            entity.MetadataJson,
            entity.CreatedAt);
}
