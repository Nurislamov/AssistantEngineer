using System.Collections.Concurrent;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public sealed class InMemoryTelegramUserStore : ITelegramUserStore
{
    private readonly ConcurrentDictionary<long, TelegramUserEntity> _users = new();
    private long _lastId;

    public Task<TelegramUserSnapshot> EnsureBootstrapOwnerAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
        var user = _users.AddOrUpdate(
            update.ChatId,
            _ => NewUser(update, TelegramUserRole.Owner, now),
            (_, existing) =>
            {
                ApplyIdentity(existing, update);
                existing.Role = TelegramUserRole.Owner;
                existing.IsEnabled = true;
                existing.IsBlocked = false;
                existing.LastSeenAt = now;
                return existing;
            });

        return Task.FromResult(ToSnapshot(user));
    }

    public Task<TelegramUserSnapshot> GetOrCreateConsumerAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
        var user = _users.AddOrUpdate(
            update.ChatId,
            _ => NewUser(update, TelegramUserRole.Consumer, now),
            (_, existing) =>
            {
                ApplyIdentity(existing, update);
                existing.LastSeenAt = now;
                return existing;
            });

        return Task.FromResult(ToSnapshot(user));
    }

    public Task<TelegramUserSnapshot?> GetByChatIdAsync(
        long chatId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.TryGetValue(chatId, out var user) ? ToSnapshot(user) : null);

    public Task<TelegramUserSnapshot?> GetByIdAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(item => item.Id == telegramUserDatabaseId);
        return Task.FromResult(user is null ? null : ToSnapshot(user));
    }

    public Task<TelegramUserSnapshot?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(item => item.TelegramUserId == telegramUserId);
        return Task.FromResult(user is null ? null : ToSnapshot(user));
    }

    public Task<TelegramUserSnapshot?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().TrimStart('@');
        var user = _users.Values.FirstOrDefault(item =>
            string.Equals(item.Username, normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user is null ? null : ToSnapshot(user));
    }

    public Task<TelegramUserPrivateContact?> GetPrivateContactAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(item => item.Id == telegramUserDatabaseId);
        return Task.FromResult(
            user is null || string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? null
                : new TelegramUserPrivateContact(user.Id, user.TelegramChatId, user.PhoneNumber));
    }

    public Task<IReadOnlyList<TelegramUserSnapshot>> ListUsersAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var users = _users.Values
            .OrderByDescending(user => user.LastSeenAt ?? user.CreatedAt)
            .ThenByDescending(user => user.CreatedAt)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(ToSnapshot)
            .ToArray();

        return Task.FromResult<IReadOnlyList<TelegramUserSnapshot>>(users);
    }

    public Task<TelegramUserOverview> GetUserOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var snapshots = _users.Values.Select(ToSnapshot).ToArray();
        var counts = Enum.GetValues<TelegramUserRole>()
            .ToDictionary(role => role, role => snapshots.Count(user => user.Role == role));
        var reachable = snapshots.Count(IsReachableForPrivateMessage);
        return Task.FromResult(new TelegramUserOverview(
            snapshots.Length,
            snapshots.Count(user => user.IsEnabled && !user.IsBlocked),
            reachable,
            snapshots.Length - reachable,
            counts));
    }

    public Task<TelegramUserListPage> GetUsersByRoleAsync(
        TelegramUserRole role,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        var roleUsers = _users.Values
            .Where(user => user.Role == role)
            .OrderBy(user => user.IsBlocked)
            .ThenByDescending(user => user.IsEnabled)
            .ThenByDescending(user => user.LastSeenAt ?? user.CreatedAt)
            .ThenBy(user => user.Id)
            .Select(ToSnapshot)
            .ToArray();
        var totalPages = Math.Max(1, (int)Math.Ceiling(roleUsers.Length / (double)pageSize));
        page = Math.Clamp(page, 0, totalPages - 1);
        var users = roleUsers
            .Skip(page * pageSize)
            .Take(pageSize)
            .Select(ToListItem)
            .ToArray();
        return Task.FromResult(new TelegramUserListPage(role, page, pageSize, roleUsers.Length, users));
    }

    public Task MarkAccessDeniedAsync(
        long chatId,
        CancellationToken cancellationToken = default)
    {
        if (_users.TryGetValue(chatId, out var user))
        {
            user.LastAccessDeniedAt = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task<TelegramUserCommandResult> AllowAsync(
        long chatId,
        TelegramUserRole role,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var user = _users.AddOrUpdate(
            chatId,
            _ => new TelegramUserEntity
            {
                Id = Interlocked.Increment(ref _lastId),
                TelegramChatId = chatId,
                Role = role,
                IsEnabled = true,
                IsBlocked = false,
                CreatedAt = now,
                LastSeenAt = now
            },
            (_, existing) =>
            {
                existing.Role = role;
                existing.IsEnabled = true;
                existing.IsBlocked = false;
                return existing;
            });

        return Task.FromResult(new TelegramUserCommandResult(true, $"Пользователь {user.TelegramChatId} разрешен с ролью {user.Role}."));
    }

    public Task<TelegramUserCommandResult> SetRoleAsync(
        long chatId,
        TelegramUserRole role,
        CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(chatId, out var user))
        {
            return Task.FromResult(new TelegramUserCommandResult(false, "Пользователь не найден."));
        }

        user.Role = role;
        return Task.FromResult(new TelegramUserCommandResult(true, $"Роль пользователя изменена на {role}."));
    }

    public Task<TelegramUserCommandResult> SetEnabledAsync(
        long chatId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(chatId, out var user))
        {
            return Task.FromResult(new TelegramUserCommandResult(false, "Пользователь не найден."));
        }

        user.IsEnabled = isEnabled;
        return Task.FromResult(new TelegramUserCommandResult(true, isEnabled ? "Доступ пользователя включен." : "Доступ пользователя выключен."));
    }

    public Task<TelegramUserCommandResult> SetBlockedAsync(
        long chatId,
        bool isBlocked,
        CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(chatId, out var user))
        {
            return Task.FromResult(new TelegramUserCommandResult(false, "Пользователь не найден."));
        }

        user.IsBlocked = isBlocked;
        return Task.FromResult(new TelegramUserCommandResult(true, isBlocked ? "Пользователь заблокирован." : "Пользователь разблокирован."));
    }

    public Task<TelegramUserCommandResult> SavePhoneAsync(
        long chatId,
        string phoneNumber,
        bool verified,
        TelegramUserPhoneNumberSource source,
        DateTimeOffset sharedAt,
        CancellationToken cancellationToken = default)
    {
        if (!_users.TryGetValue(chatId, out var user))
        {
            return Task.FromResult(new TelegramUserCommandResult(false, "Пользователь не найден."));
        }

        user.PhoneNumber = phoneNumber;
        user.PhoneNumberVerified = verified;
        user.PhoneNumberSource = source;
        user.PhoneNumberSharedAt = sharedAt;
        return Task.FromResult(new TelegramUserCommandResult(true, "Номер телефона сохранен."));
    }

    private TelegramUserEntity NewUser(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserRole role,
        DateTimeOffset now) =>
        new()
        {
            Id = Interlocked.Increment(ref _lastId),
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

    private static TelegramUserListItem ToListItem(TelegramUserSnapshot user) =>
        new(
            user.Id,
            user.TelegramChatId,
            user.TelegramUserId,
            user.Username,
            user.FirstName,
            user.LastName,
            user.Role,
            HasPrivateChat(user),
            user.IsEnabled,
            user.IsBlocked,
            IsReachableForPrivateMessage(user),
            user.CreatedAt,
            user.LastSeenAt);

    private static bool HasPrivateChat(TelegramUserSnapshot user) =>
        user.TelegramChatId > 0;

    private static bool IsReachableForPrivateMessage(TelegramUserSnapshot user) =>
        HasPrivateChat(user) && user.IsEnabled && !user.IsBlocked;
}
