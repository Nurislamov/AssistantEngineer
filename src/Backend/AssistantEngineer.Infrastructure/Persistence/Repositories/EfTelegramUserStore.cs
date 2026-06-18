using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Persistence.Repositories;

public sealed class EfTelegramUserStore : ITelegramUserStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EfTelegramUserStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<TelegramUserSnapshot> EnsureBootstrapOwnerAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
        var user = await FindByChatIdAsync(context, update.ChatId, cancellationToken);
        if (user is null)
        {
            user = NewUser(update, TelegramUserRole.Owner, now);
            await context.TelegramUsers.AddAsync(user, cancellationToken);
        }
        else
        {
            ApplyIdentity(user, update);
            user.Role = TelegramUserRole.Owner;
            user.IsEnabled = true;
            user.IsBlocked = false;
            user.LastSeenAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(user);
    }

    public async Task<TelegramUserSnapshot> GetOrCreateConsumerAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
        var user = await FindByChatIdAsync(context, update.ChatId, cancellationToken);
        if (user is null)
        {
            user = NewUser(update, TelegramUserRole.Consumer, now);
            await context.TelegramUsers.AddAsync(user, cancellationToken);
        }
        else
        {
            ApplyIdentity(user, update);
            user.LastSeenAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        return ToSnapshot(user);
    }

    public async Task<TelegramUserSnapshot?> GetByChatIdAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await context.TelegramUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.TelegramChatId == chatId, cancellationToken);

        return user is null ? null : ToSnapshot(user);
    }

    public async Task<TelegramUserSnapshot?> GetByIdAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await context.TelegramUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == telegramUserDatabaseId, cancellationToken);
        return user is null ? null : ToSnapshot(user);
    }

    public async Task<TelegramUserSnapshot?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await context.TelegramUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.TelegramUserId == telegramUserId, cancellationToken);
        return user is null ? null : ToSnapshot(user);
    }

    public async Task<TelegramUserSnapshot?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalized = username.Trim().TrimStart('@').ToLower();
        var user = await context.TelegramUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Username != null && item.Username.ToLower() == normalized,
                cancellationToken);
        return user is null ? null : ToSnapshot(user);
    }

    public async Task<TelegramUserPrivateContact?> GetPrivateContactAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.TelegramUsers
            .AsNoTracking()
            .Where(item => item.Id == telegramUserDatabaseId && item.PhoneNumber != null)
            .Select(item => new TelegramUserPrivateContact(item.Id, item.TelegramChatId, item.PhoneNumber!))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TelegramUserSnapshot>> ListUsersAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = await context.TelegramUsers
            .AsNoTracking()
            .OrderByDescending(user => user.LastSeenAt ?? user.CreatedAt)
            .ThenByDescending(user => user.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArrayAsync(cancellationToken);

        return users.Select(ToSnapshot).ToArray();
    }

    public async Task MarkAccessDeniedAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await FindByChatIdAsync(context, chatId, cancellationToken);
        if (user is null)
        {
            return;
        }

        user.LastAccessDeniedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TelegramUserCommandResult> AllowAsync(
        long chatId,
        TelegramUserRole role,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await FindByChatIdAsync(context, chatId, cancellationToken);
        if (user is null)
        {
            var now = DateTimeOffset.UtcNow;
            user = new TelegramUserEntity
            {
                TelegramChatId = chatId,
                Role = role,
                IsEnabled = true,
                IsBlocked = false,
                CreatedAt = now,
                LastSeenAt = now
            };
            await context.TelegramUsers.AddAsync(user, cancellationToken);
        }
        else
        {
            user.Role = role;
            user.IsEnabled = true;
            user.IsBlocked = false;
        }

        await context.SaveChangesAsync(cancellationToken);
        return new TelegramUserCommandResult(true, $"Пользователь {chatId} разрешен с ролью {role}.");
    }

    public async Task<TelegramUserCommandResult> SetRoleAsync(
        long chatId,
        TelegramUserRole role,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await FindByChatIdAsync(context, chatId, cancellationToken);
        if (user is null)
        {
            return new TelegramUserCommandResult(false, "Пользователь не найден.");
        }

        user.Role = role;
        await context.SaveChangesAsync(cancellationToken);
        return new TelegramUserCommandResult(true, $"Роль пользователя изменена на {role}.");
    }

    public async Task<TelegramUserCommandResult> SetEnabledAsync(
        long chatId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await FindByChatIdAsync(context, chatId, cancellationToken);
        if (user is null)
        {
            return new TelegramUserCommandResult(false, "Пользователь не найден.");
        }

        user.IsEnabled = isEnabled;
        await context.SaveChangesAsync(cancellationToken);
        return new TelegramUserCommandResult(true, isEnabled ? "Доступ пользователя включен." : "Доступ пользователя выключен.");
    }

    public async Task<TelegramUserCommandResult> SetBlockedAsync(
        long chatId,
        bool isBlocked,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await FindByChatIdAsync(context, chatId, cancellationToken);
        if (user is null)
        {
            return new TelegramUserCommandResult(false, "Пользователь не найден.");
        }

        user.IsBlocked = isBlocked;
        await context.SaveChangesAsync(cancellationToken);
        return new TelegramUserCommandResult(true, isBlocked ? "Пользователь заблокирован." : "Пользователь разблокирован.");
    }

    public async Task<TelegramUserCommandResult> SavePhoneAsync(
        long chatId,
        string phoneNumber,
        bool verified,
        TelegramUserPhoneNumberSource source,
        DateTimeOffset sharedAt,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await FindByChatIdAsync(context, chatId, cancellationToken);
        if (user is null)
        {
            return new TelegramUserCommandResult(false, "Пользователь не найден.");
        }

        user.PhoneNumber = phoneNumber;
        user.PhoneNumberVerified = verified;
        user.PhoneNumberSource = source;
        user.PhoneNumberSharedAt = sharedAt;
        await context.SaveChangesAsync(cancellationToken);
        return new TelegramUserCommandResult(true, "Номер телефона сохранен.");
    }

    private static Task<TelegramUserEntity?> FindByChatIdAsync(
        AppDbContext context,
        long chatId,
        CancellationToken cancellationToken) =>
        context.TelegramUsers.FirstOrDefaultAsync(user => user.TelegramChatId == chatId, cancellationToken);

    private static TelegramUserEntity NewUser(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserRole role,
        DateTimeOffset now) =>
        new()
        {
            TelegramChatId = update.ChatId,
            TelegramUserId = update.UserId,
            Username = Normalize(update.Username),
            FirstName = Normalize(update.FirstName),
            LastName = Normalize(update.LastName),
            Role = role,
            IsEnabled = true,
            IsBlocked = false,
            CreatedAt = now,
            LastSeenAt = now
        };

    private static void ApplyIdentity(
        TelegramUserEntity user,
        EquipmentDiagnosticTelegramUpdate update)
    {
        user.TelegramUserId = update.UserId ?? user.TelegramUserId;
        user.Username = Normalize(update.Username) ?? user.Username;
        user.FirstName = Normalize(update.FirstName) ?? user.FirstName;
        user.LastName = Normalize(update.LastName) ?? user.LastName;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TelegramUserSnapshot ToSnapshot(TelegramUserEntity user) =>
        new(
            user.Id,
            user.TelegramChatId,
            user.TelegramUserId,
            user.Username,
            user.FirstName,
            user.LastName,
            user.Role,
            user.IsEnabled,
            user.IsBlocked,
            user.PhoneNumberVerified,
            !string.IsNullOrWhiteSpace(user.PhoneNumber),
            user.PhoneNumberSource,
            user.CreatedAt,
            user.LastSeenAt,
            user.LastAccessDeniedAt);
}
