using System.Text;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public sealed record TelegramAdminUserManagementResult(
    string Text,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    string? CallbackAnswerText = null,
    bool SuppressOutbound = false);

public sealed class TelegramAdminUserManagementService
{
    private const int ListLimit = 10;

    private readonly ITelegramUserStore _userStore;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;
    private readonly TelegramUserAuditEventService? _auditService;
    private readonly ILogger<TelegramAdminUserManagementService> _logger;

    public TelegramAdminUserManagementService(
        ITelegramUserStore userStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        TelegramDisplayTimeFormatter timeFormatter,
        TelegramUserAuditEventService? auditService = null,
        ILogger<TelegramAdminUserManagementService>? logger = null)
    {
        _userStore = userStore;
        _outboundClient = outboundClient;
        _timeFormatter = timeFormatter;
        _auditService = auditService;
        _logger = logger ?? NullLogger<TelegramAdminUserManagementService>.Instance;
    }

    public static bool IsCommand(string? text)
    {
        var command = text?.Trim().Split(' ', 2)[0].Split('@', 2)[0];
        return command is not null &&
            (command.Equals("/admin_users", StringComparison.OrdinalIgnoreCase) ||
             command.Equals("/admin_pending", StringComparison.OrdinalIgnoreCase) ||
             command.Equals("/admin_audit", StringComparison.OrdinalIgnoreCase) ||
             command.Equals("/engineers", StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TelegramAdminUserManagementResult> HandleCommandAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!CanManage(access.User))
        {
            return Result("Команда недоступна.");
        }

        var command = update.Text?.Trim().Split(' ', 2)[0].Split('@', 2)[0].ToLowerInvariant();
        return command switch
        {
            "/admin_pending" => await RenderListAsync(AdminListKind.Pending, cancellationToken),
            "/engineers" => await RenderListAsync(AdminListKind.Engineers, cancellationToken),
            "/admin_audit" => Result(_auditService is null
                ? "Аудит управления пользователями недоступен."
                : await _auditService.FormatLatestAsync(cancellationToken)),
            _ => await RenderListAsync(AdminListKind.All, cancellationToken)
        };
    }

    public async Task<TelegramAdminUserManagementResult> HandleCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        var actor = await ResolveCallbackActorAsync(update, cancellationToken);
        var parsed = TryParseCallback(update.CallbackData, out var action, out var targetId, out var role);
        if (!CanManage(actor))
        {
            await AppendDeniedAsync(
                actor,
                targetId,
                ActionName(action),
                actor is { Role: TelegramUserRole.Owner or TelegramUserRole.Admin }
                    ? "disabled_or_blocked_actor"
                    : "insufficient_permissions",
                update.ReceivedAt ?? DateTimeOffset.UtcNow,
                cancellationToken);
            return Callback("Команда недоступна.", "Нет доступа");
        }

        if (!parsed)
        {
            var invalid = ClassifyInvalidCallback(update.CallbackData);
            await AppendDeniedAsync(
                actor,
                invalid.TargetId,
                invalid.Action,
                invalid.Reason,
                update.ReceivedAt ?? DateTimeOffset.UtcNow,
                cancellationToken);
            return Callback("Действие недоступно или устарело.", "Ошибка действия");
        }

        TelegramAdminUserManagementResult result;
        if (action is "all" or "p" or "eng")
        {
            var kind = action switch
            {
                "p" => AdminListKind.Pending,
                "eng" => AdminListKind.Engineers,
                _ => AdminListKind.All
            };
            result = await RenderListAsync(kind, cancellationToken);
        }
        else if (action == "v" && targetId is not null)
        {
            result = await RenderUserAsync(targetId.Value, actor!, cancellationToken);
        }
        else
        {
            result = await MutateAsync(
                action,
                targetId,
                role,
                actor!,
                update.ReceivedAt ?? DateTimeOffset.UtcNow,
                cancellationToken);
        }

        if (result.ReplyMarkup is not null)
        {
            var edited = await TryEditAsync(update, result.Text, result.ReplyMarkup, cancellationToken);
            if (!edited)
            {
                await SendFallbackAsync(update.ChatId, result.Text, result.ReplyMarkup, cancellationToken);
            }
        }

        return result with { SuppressOutbound = true };
    }

    private async Task<TelegramUserSnapshot?> ResolveCallbackActorAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken)
    {
        TelegramUserSnapshot? userByTelegramId = null;
        if (update.UserId is not null)
        {
            userByTelegramId = await _userStore.GetByTelegramUserIdAsync(update.UserId.Value, cancellationToken);
            if (CanManage(userByTelegramId))
            {
                return userByTelegramId;
            }
        }

        if (!IsPrivateChat(update.ChatType))
        {
            return userByTelegramId;
        }

        var chatUser = await _userStore.GetByChatIdAsync(update.ChatId, cancellationToken);
        if (chatUser is null)
        {
            return userByTelegramId;
        }

        if (update.UserId is null)
        {
            return chatUser;
        }

        if (userByTelegramId is null ||
            userByTelegramId.Id == chatUser.Id ||
            CanManage(chatUser) && !CanManage(userByTelegramId))
        {
            return await _userStore.GetOrCreateConsumerAsync(update, cancellationToken);
        }

        return userByTelegramId;
    }

    private async Task<TelegramAdminUserManagementResult> MutateAsync(
        string action,
        long? targetId,
        TelegramUserRole? requestedRole,
        TelegramUserSnapshot actor,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (targetId is null)
        {
            await AppendDeniedAsync(
                actor,
                null,
                ActionName(action),
                "target_not_found",
                createdAt,
                cancellationToken);
            return Callback("Пользователь не найден.", "Ошибка действия");
        }

        var target = await _userStore.GetByIdAsync(targetId.Value, cancellationToken);
        if (target is null)
        {
            await AppendDeniedAsync(
                actor,
                targetId,
                ActionName(action),
                "target_not_found",
                createdAt,
                cancellationToken);
            return Callback("Пользователь не найден.", "Ошибка действия");
        }

        var denied = ValidateMutation(actor, target, action, requestedRole);
        if (denied is not null)
        {
            await AppendDeniedAsync(
                actor,
                target.Id,
                ActionName(action),
                denied.Value.Reason,
                createdAt,
                cancellationToken);
            return Callback(
                denied.Value.Message,
                denied.Value.Message.StartsWith("Owner", StringComparison.Ordinal)
                    ? denied.Value.Message
                    : "Нет доступа");
        }

        TelegramUserCommandResult mutation;
        string answer;
        if (action == "r" && requestedRole is not null)
        {
            mutation = await _userStore.SetRoleAsync(target.TelegramChatId, requestedRole.Value, cancellationToken);
            answer = "Роль обновлена";
        }
        else if (action is "b" or "u")
        {
            var blocked = action == "b";
            mutation = await _userStore.SetBlockedAsync(target.TelegramChatId, blocked, cancellationToken);
            answer = blocked ? "Пользователь заблокирован" : "Пользователь разблокирован";
        }
        else if (action is "d" or "en")
        {
            var enabled = action == "en";
            mutation = await _userStore.SetEnabledAsync(target.TelegramChatId, enabled, cancellationToken);
            answer = enabled ? "Пользователь включён" : "Пользователь отключён";
        }
        else
        {
            await AppendDeniedAsync(
                actor,
                target.Id,
                ActionName(action),
                "unsupported_action",
                createdAt,
                cancellationToken);
            return Callback("Действие недоступно или устарело.", "Ошибка действия");
        }

        if (!mutation.Succeeded)
        {
            await AppendDeniedAsync(
                actor,
                target.Id,
                ActionName(action),
                "target_not_found",
                createdAt,
                cancellationToken);
            return Callback("Команда не выполнена.", "Ошибка действия");
        }

        var updated = await _userStore.GetByIdAsync(target.Id, cancellationToken) ?? target;
        await AppendSuccessAsync(action, actor, target, updated, createdAt, cancellationToken);
        var card = await RenderUserAsync(updated.Id, actor, cancellationToken);
        return card with { CallbackAnswerText = answer };
    }

    private static (string Reason, string Message)? ValidateMutation(
        TelegramUserSnapshot actor,
        TelegramUserSnapshot target,
        string action,
        TelegramUserRole? requestedRole)
    {
        var destructive = action is "b" or "d" ||
            action == "r" && requestedRole != target.Role;
        if (actor.Id == target.Id && destructive)
        {
            return ("self_action_denied", "Нельзя изменить собственный доступ через эту кнопку.");
        }
        if (target.Role == TelegramUserRole.Owner && destructive)
        {
            return ("owner_protected", "Owner защищён от этого действия.");
        }
        if (actor.Role == TelegramUserRole.Admin)
        {
            if (target.Role is TelegramUserRole.Owner or TelegramUserRole.Admin)
            {
                return ("insufficient_permissions", "Admin не может управлять Owner или Admin.");
            }
            if (requestedRole is TelegramUserRole.Admin or TelegramUserRole.Owner)
            {
                return ("insufficient_permissions", "Назначить Admin может только Owner.");
            }
        }
        if (requestedRole == TelegramUserRole.Owner)
        {
            return ("invalid_role", "Назначение Owner через кнопки недоступно.");
        }

        return null;
    }

    private async Task<TelegramAdminUserManagementResult> RenderListAsync(
        AdminListKind kind,
        CancellationToken cancellationToken)
    {
        var users = await _userStore.ListUsersAsync(ListLimit, cancellationToken);
        users = kind switch
        {
            AdminListKind.Pending => users
                .Where(user => user.Role == TelegramUserRole.Consumer && user.IsEnabled && !user.IsBlocked)
                .ToArray(),
            AdminListKind.Engineers => users
                .Where(user => TelegramUserRolePolicy.IsServiceEngineerRole(user.Role))
                .ToArray(),
            _ => users
        };

        var title = kind switch
        {
            AdminListKind.Pending => "Новые пользователи",
            AdminListKind.Engineers => "Сервис-инженеры",
            _ => "Пользователи Telegram"
        };
        var builder = new StringBuilder(title);
        if (users.Count == 0)
        {
            builder.Append("\n\nСписок пуст.");
        }
        else
        {
            foreach (var user in users)
            {
                builder.AppendLine();
                builder.AppendLine($"• {DisplayLabel(user)} — {RoleLabel(user.Role)}");
                builder.Append(
                    $"  Доступ: {(user.IsEnabled ? "включён" : "отключён")}; " +
                    $"блокировка: {(user.IsBlocked ? "да" : "нет")}; " +
                    $"телефон: {(user.HasPhoneNumber ? "сохранён" : "не сохранён")}; " +
                    $"активность: {(user.LastSeenAt is null ? "неизвестна" : _timeFormatter.FormatRelative(user.LastSeenAt.Value))}");
            }
        }

        var rows = users
            .Select(user => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
            [
                Button($"Открыть: {ShortLabel(user)}", $"au:v:{user.Id}")
            ])
            .Append(
            [
                Button("Пользователи", "au:all"),
                Button("Новые", "au:p"),
                Button("Сервис-инженеры", "au:eng")
            ])
            .Append([Button("Обновить", CallbackFor(kind))])
            .ToArray();
        return Result(builder.ToString(), InlineKeyboard(rows));
    }

    private async Task<TelegramAdminUserManagementResult> RenderUserAsync(
        long userId,
        TelegramUserSnapshot actor,
        CancellationToken cancellationToken)
    {
        var user = await _userStore.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Callback("Пользователь не найден.", "Ошибка действия");
        }

        var text =
            "Пользователь Telegram\n\n" +
            $"Имя: {DisplayName(user)}\n" +
            $"Username: {(string.IsNullOrWhiteSpace(user.Username) ? "не указан" : $"@{user.Username.Trim().TrimStart('@')}")}\n" +
            $"Роль: {RoleLabel(user.Role)}\n" +
            $"Доступ: {(user.IsEnabled ? "включён" : "отключён")}\n" +
            $"Блокировка: {(user.IsBlocked ? "да" : "нет")}\n" +
            $"Телефон: {(user.HasPhoneNumber ? "сохранён" : "не сохранён")}\n" +
            $"Последняя активность: {(user.LastSeenAt is null ? "неизвестна" : _timeFormatter.FormatRelative(user.LastSeenAt.Value))}";

        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>();
        var canManageTarget = actor.Role == TelegramUserRole.Owner ||
            user.Role is TelegramUserRole.Consumer or TelegramUserRole.Engineer or TelegramUserRole.Installer;
        var isSelf = actor.Id == user.Id;
        var protectedOwner = user.Role == TelegramUserRole.Owner;

        if (canManageTarget && !isSelf && !protectedOwner)
        {
            var roleButtons = new List<EquipmentDiagnosticTelegramInlineKeyboardButton>();
            if (user.Role != TelegramUserRole.Engineer)
            {
                roleButtons.Add(Button("Сделать сервис-инженером", $"au:r:{user.Id}:e"));
            }
            if (user.Role != TelegramUserRole.Installer)
            {
                roleButtons.Add(Button("Сделать монтажником", $"au:r:{user.Id}:i"));
            }
            if (user.Role != TelegramUserRole.Consumer)
            {
                roleButtons.Add(Button("Сделать клиентом", $"au:r:{user.Id}:c"));
            }
            if (actor.Role == TelegramUserRole.Owner && user.Role != TelegramUserRole.Admin)
            {
                roleButtons.Add(Button("Сделать админом", $"au:r:{user.Id}:a"));
            }
            if (roleButtons.Count > 0)
            {
                foreach (var row in roleButtons.Chunk(2))
                {
                    rows.Add(row);
                }
            }

            rows.Add(
            [
                user.IsBlocked
                    ? Button("Разблокировать", $"au:u:{user.Id}")
                    : Button("Заблокировать", $"au:b:{user.Id}"),
                user.IsEnabled
                    ? Button("Отключить", $"au:d:{user.Id}")
                    : Button("Включить", $"au:en:{user.Id}")
            ]);
        }

        rows.Add(
        [
            Button("Назад", "au:all"),
            Button("Обновить", $"au:v:{user.Id}")
        ]);
        return Result(text, InlineKeyboard(rows));
    }

    private async Task<bool> TryEditAsync(
        EquipmentDiagnosticTelegramUpdate update,
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup,
        CancellationToken cancellationToken)
    {
        if (update.MessageId is null)
        {
            return false;
        }

        try
        {
            var result = await _outboundClient.EditMessageTextAsync(
                update.ChatId,
                update.MessageId.Value,
                text,
                replyMarkup,
                cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Telegram admin user card edit failed; committed user state remains unchanged.");
            }
            return result.Succeeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram admin user card edit failed; committed user state remains unchanged. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return false;
        }
    }

    private async Task SendFallbackAsync(
        long chatId,
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _outboundClient.SendMessageAsync(
                chatId,
                text,
                parseMode: null,
                disableWebPagePreview: true,
                replyMarkup,
                cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Telegram admin user card fallback send failed.");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram admin user card fallback send failed. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
        }
    }

    private async Task AppendSuccessAsync(
        string action,
        TelegramUserSnapshot actor,
        TelegramUserSnapshot before,
        TelegramUserSnapshot after,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        if (_auditService is null)
        {
            return;
        }

        var eventType = action switch
        {
            "r" => TelegramUserAuditEventType.RoleChanged,
            "en" => TelegramUserAuditEventType.UserEnabled,
            "d" => TelegramUserAuditEventType.UserDisabled,
            "b" => TelegramUserAuditEventType.UserBlocked,
            "u" => TelegramUserAuditEventType.UserUnblocked,
            _ => TelegramUserAuditEventType.UserActionDenied
        };
        await _auditService.AppendSafeAsync(
            new TelegramUserAuditEventCreate(
                eventType,
                actor.Id,
                before.Id,
                before.Role,
                after.Role,
                before.IsEnabled,
                after.IsEnabled,
                before.IsBlocked,
                after.IsBlocked,
                true,
                null,
                JsonSerializer.Serialize(new { action = ActionName(action) }),
                createdAt),
            cancellationToken);
    }

    private Task AppendDeniedAsync(
        TelegramUserSnapshot? actor,
        long? targetId,
        string action,
        string reason,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken) =>
        _auditService?.AppendSafeAsync(
            new TelegramUserAuditEventCreate(
                TelegramUserAuditEventType.UserActionDenied,
                actor?.Id,
                targetId,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                null,
                JsonSerializer.Serialize(new { action, reason }),
                createdAt),
            cancellationToken) ?? Task.CompletedTask;

    private static (string Action, string Reason, long? TargetId) ClassifyInvalidCallback(string? data)
    {
        var parts = data?.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        long? targetId = parts.Length >= 3 && long.TryParse(parts[2], out var parsedId) && parsedId > 0
            ? parsedId
            : null;
        if (parts.Length >= 2 && parts[1] == "r")
        {
            return ("role", "invalid_role", targetId);
        }

        return ("unsupported", "unsupported_action", targetId);
    }

    private static string ActionName(string action) =>
        action switch
        {
            "r" => "role",
            "en" => "enable",
            "d" => "disable",
            "b" => "block",
            "u" => "unblock",
            _ => "unsupported"
        };

    private static bool TryParseCallback(
        string? data,
        out string action,
        out long? targetId,
        out TelegramUserRole? role)
    {
        action = string.Empty;
        targetId = null;
        role = null;
        if (string.IsNullOrWhiteSpace(data) || Encoding.UTF8.GetByteCount(data) > 64)
        {
            return false;
        }

        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2 || parts[0] != "au")
        {
            return false;
        }

        action = parts[1];
        if (action is "all" or "p" or "eng")
        {
            return parts.Length == 2;
        }
        if (parts.Length < 3 || !long.TryParse(parts[2], out var parsedId) || parsedId <= 0)
        {
            return false;
        }
        targetId = parsedId;
        if (action == "r")
        {
            if (parts.Length != 4)
            {
                return false;
            }
            role = parts[3] switch
            {
                "e" => TelegramUserRole.Engineer,
                "i" => TelegramUserRole.Installer,
                "a" => TelegramUserRole.Admin,
                "c" => TelegramUserRole.Consumer,
                _ => null
            };
            return role is not null;
        }

        return parts.Length == 3 && action is ("v" or "b" or "u" or "d" or "en");
    }

    private static bool CanManage(TelegramUserSnapshot? user) =>
        user is { IsEnabled: true, IsBlocked: false } &&
        TelegramUserRolePolicy.CanManageTelegramUsers(user.Role);

    private static bool IsPrivateChat(string? chatType) =>
        string.IsNullOrWhiteSpace(chatType) ||
        chatType.Equals("private", StringComparison.OrdinalIgnoreCase);

    private static string CallbackFor(AdminListKind kind) =>
        kind switch
        {
            AdminListKind.Pending => "au:p",
            AdminListKind.Engineers => "au:eng",
            _ => "au:all"
        };

    private static string DisplayLabel(TelegramUserSnapshot user)
    {
        var name = DisplayName(user);
        return string.IsNullOrWhiteSpace(user.Username)
            ? name
            : $"{name} (@{user.Username.Trim().TrimStart('@')})";
    }

    private static string ShortLabel(TelegramUserSnapshot user) =>
        string.IsNullOrWhiteSpace(user.Username)
            ? DisplayName(user)
            : $"@{user.Username.Trim().TrimStart('@')}";

    private static string DisplayName(TelegramUserSnapshot user)
    {
        var name = string.Join(
            " ",
            new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? "Без имени" : name;
    }

    private static string RoleLabel(TelegramUserRole role) =>
        TelegramUserRolePolicy.DisplayName(role);

    private static EquipmentDiagnosticTelegramInlineKeyboardButton Button(string text, string callbackData) =>
        new(text, callbackData);

    private static EquipmentDiagnosticTelegramReplyMarkup InlineKeyboard(
        IReadOnlyList<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>> rows) =>
        new(InlineKeyboard: rows);

    private static TelegramAdminUserManagementResult Result(
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null) =>
        new(text, replyMarkup);

    private static TelegramAdminUserManagementResult Callback(string text, string answer) =>
        new(text, CallbackAnswerText: answer, SuppressOutbound: true);

    private enum AdminListKind
    {
        All,
        Pending,
        Engineers
    }
}
