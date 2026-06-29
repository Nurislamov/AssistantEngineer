using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramManualFileBindingStore : ITelegramManualFileBindingStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramManualFileBindingStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramManualFileBinding?> GetAsync(
        string manualId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var binding = await context.TelegramManualBindings
            .AsNoTracking()
            .Where(item => item.IsActive && item.ManualId == manualId)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return binding is null ? null : ToBinding(binding);
    }

    public async Task<TelegramManualFileBinding?> GetBySeriesAsync(
        string brand,
        string series,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedBrand = Normalize(brand);
        var normalizedSeries = Normalize(series);
        var binding = await context.TelegramManualBindings
            .AsNoTracking()
            .Where(item =>
                item.IsActive &&
                item.Brand != null &&
                item.Series != null &&
                item.Brand.ToLower() == normalizedBrand &&
                item.Series.ToLower() == normalizedSeries)
            .OrderByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return binding is null ? null : ToBinding(binding);
    }

    public async Task<IReadOnlyList<TelegramManualFileBinding>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bindings = await context.TelegramManualBindings
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Brand)
            .ThenBy(item => item.Series)
            .ThenBy(item => item.ManualId)
            .ToArrayAsync(cancellationToken);
        return bindings.Select(ToBinding).ToArray();
    }

    public async Task UpsertAsync(
        TelegramManualFileBinding binding,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await context.TelegramManualBindings
            .FirstOrDefaultAsync(
                item => item.IsActive && item.ManualId == binding.ManualId,
                cancellationToken);
        Upsert(existing, context, binding);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertSeriesAsync(
        TelegramManualFileBinding binding,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedBrand = Normalize(binding.Brand);
        var normalizedSeries = Normalize(binding.Series);
        var existing = await context.TelegramManualBindings
            .FirstOrDefaultAsync(
                item =>
                    item.IsActive &&
                    item.Brand != null &&
                    item.Series != null &&
                    item.Brand.ToLower() == normalizedBrand &&
                    item.Series.ToLower() == normalizedSeries,
                cancellationToken);
        Upsert(existing, context, binding);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RemoveAsync(
        string manualId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var binding = await context.TelegramManualBindings
            .FirstOrDefaultAsync(
                item => item.IsActive && item.ManualId == manualId,
                cancellationToken);
        if (binding is null)
        {
            return false;
        }

        binding.IsActive = false;
        binding.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Upsert(
        TelegramManualBindingEntity? existing,
        AppDbContext context,
        TelegramManualFileBinding binding)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = existing ?? new TelegramManualBindingEntity
        {
            CreatedAt = binding.RegisteredAtUtc == default ? now : binding.RegisteredAtUtc
        };
        entity.ManualId = EmptyToNull(binding.ManualId);
        entity.Brand = EmptyToNull(binding.Brand);
        entity.Series = EmptyToNull(binding.Series);
        entity.TelegramFileId = binding.TelegramFileId;
        entity.TelegramFileUniqueId = EmptyToNull(binding.TelegramFileUniqueId);
        entity.FileName = EmptyToNull(binding.OriginalFileName);
        entity.ContentType = EmptyToNull(binding.ContentType);
        entity.FileSize = binding.FileSize;
        entity.UploadedByTelegramUserId = binding.UploadedByTelegramUserId;
        entity.UploadedByTelegramChatId = binding.UploadedByTelegramChatId;
        entity.RegisteredByRole = EmptyToNull(binding.RegisteredByRole);
        entity.Source = string.IsNullOrWhiteSpace(binding.Source) ? "TelegramManualBind" : binding.Source.Trim();
        entity.IsActive = binding.IsActive;
        entity.UpdatedAt = now;

        if (existing is null)
        {
            context.TelegramManualBindings.Add(entity);
        }
    }

    private static TelegramManualFileBinding ToBinding(TelegramManualBindingEntity entity) =>
        new(
            entity.ManualId ?? string.Empty,
            entity.TelegramFileId,
            entity.FileName,
            entity.ContentType,
            entity.CreatedAt,
            entity.Source,
            entity.RegisteredByRole,
            entity.TelegramFileUniqueId,
            entity.FileSize,
            entity.Brand,
            entity.Series,
            entity.UploadedByTelegramUserId,
            entity.UploadedByTelegramChatId,
            entity.IsActive,
            entity.UpdatedAt);

    private static string Normalize(string? value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
