using System.Globalization;
using System.Text;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public sealed class TelegramUserOverviewService
{
    public const string UsersButton = "👥 Пользователи";
    public const string UsersOverviewCallback = "usr:stats";
    private const string CallbackPrefix = "usr:";
    private const string RolePrefix = "usr:r:";
    private const int PageSize = 10;

    private readonly ITelegramUserStore _userStore;
    private readonly TelegramUserManagementService _managementService;

    public TelegramUserOverviewService(
        ITelegramUserStore userStore,
        TelegramUserManagementService? managementService = null)
    {
        _userStore = userStore;
        _managementService = managementService ?? new TelegramUserManagementService(userStore);
    }

    public static bool IsCallback(string? callbackData) =>
        callbackData?.StartsWith(CallbackPrefix, StringComparison.Ordinal) == true;

    public async Task<TelegramUserOverviewResult> HandleCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!IsOwner(access))
        {
            return Denied();
        }

        var callback = update.CallbackData ?? string.Empty;
        if (string.Equals(callback, UsersOverviewCallback, StringComparison.Ordinal))
        {
            return await BuildOverviewAsync(cancellationToken);
        }

        if (TryParseRolePage(callback, out var role, out var page))
        {
            return await BuildRolePageAsync(role, page, cancellationToken);
        }

        if (TryParseUserAction(callback, "view", out var userId))
        {
            return await BuildUserCardAsync(userId, access.User!.Id, cancellationToken);
        }

        if (TryParseUserAction(callback, "role", out userId))
        {
            return await BuildRolePickerAsync(userId, cancellationToken);
        }

        if (TryParseRoleAction(callback, "set", out userId, out role))
        {
            return await BuildRoleConfirmationAsync(userId, role, cancellationToken);
        }

        if (TryParseRoleAction(callback, "confirm:set", out userId, out role))
        {
            var result = await _managementService.ChangeUserRoleAsync(
                userId,
                role,
                access.User!.Id,
                cancellationToken);
            return await MutationResultAsync(result, access.User.Id, "Уровень обновлён", cancellationToken);
        }

        if (TryParseUserAction(callback, "block", out userId))
        {
            return await BuildBlockConfirmationAsync(userId, block: true, cancellationToken);
        }

        if (TryParseUserAction(callback, "unblock", out userId))
        {
            return await BuildBlockConfirmationAsync(userId, block: false, cancellationToken);
        }

        if (TryParseUserAction(callback, "confirm:block", out userId))
        {
            var result = await _managementService.BlockUserAsync(userId, access.User!.Id, cancellationToken);
            return await MutationResultAsync(result, access.User.Id, "Пользователь заблокирован", cancellationToken);
        }

        if (TryParseUserAction(callback, "confirm:unblock", out userId))
        {
            var result = await _managementService.UnblockUserAsync(userId, access.User!.Id, cancellationToken);
            return await MutationResultAsync(result, access.User.Id, "Пользователь разблокирован", cancellationToken);
        }

        return Stale();
    }

    public async Task<TelegramUserOverviewResult> BuildOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        var overview = await _userStore.GetUserOverviewAsync(cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("👥 Пользователи");
        builder.AppendLine();
        builder.AppendLine($"Всего пользователей: {overview.TotalCount}");
        builder.AppendLine($"Активные: {overview.ActiveCount}");
        builder.AppendLine($"Доступны для будущей рассылки: {overview.BroadcastReachableCount}");
        builder.AppendLine($"Недоступны для личных сообщений: {overview.BroadcastUnavailableCount}");
        builder.AppendLine();
        builder.AppendLine("По ролям:");
        foreach (var role in Enum.GetValues<TelegramUserRole>())
        {
            builder.AppendLine($"{RoleTitle(role)}: {overview.CountsByRole.GetValueOrDefault(role)}");
        }

        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>
        {
            new[] { Button("📊 Обновить", UsersOverviewCallback) }
        };
        rows.AddRange(Enum.GetValues<TelegramUserRole>()
            .Select(role => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [Button(RoleTitle(role), RoleCallback(role, 0))]));
        rows.Add([Button("Назад", "lib:open")]);

        return new(
            builder.ToString().TrimEnd(),
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Пользователи");
    }

    private async Task<TelegramUserOverviewResult> BuildRolePageAsync(
        TelegramUserRole role,
        int page,
        CancellationToken cancellationToken)
    {
        var result = await _userStore.GetUsersByRoleAsync(role, page, PageSize, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine($"👥 Пользователи — {RoleTitle(role)}");
        builder.AppendLine();
        builder.AppendLine($"Всего: {result.TotalCount}");
        builder.AppendLine($"Страница {result.Page + 1}/{result.TotalPages}");
        builder.AppendLine();

        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>();
        if (result.Users.Count == 0)
        {
            builder.AppendLine("Пользователей с этой ролью пока нет.");
        }
        else
        {
            for (var index = 0; index < result.Users.Count; index++)
            {
                var user = result.Users[index];
                var number = result.Page * result.PageSize + index + 1;
                builder.AppendLine($"{number}. {UserLabel(user)}");
                builder.AppendLine($"   Роль: {RoleTitle(user.Role)}");
                builder.AppendLine($"   Статус: {StatusLabel(user)}");
                builder.AppendLine($"   Рассылка: {YesNo(user.IsReachableForPrivateMessage)}");
                if (index + 1 < result.Users.Count)
                {
                    builder.AppendLine();
                }

                rows.Add([Button($"{number}) {ShortUserLabel(user)}", $"usr:view:{user.TelegramUserId}")]);
            }
        }

        var navigation = new List<EquipmentDiagnosticTelegramInlineKeyboardButton>();
        if (result.Page > 0)
        {
            navigation.Add(Button("⬅️ Назад", RoleCallback(role, result.Page - 1)));
        }
        if (result.Page + 1 < result.TotalPages)
        {
            navigation.Add(Button("➡️ Далее", RoleCallback(role, result.Page + 1)));
        }
        if (navigation.Count > 0)
        {
            rows.Add(navigation);
        }
        rows.Add([Button("Назад к пользователям", UsersOverviewCallback)]);

        return new(
            builder.ToString().TrimEnd(),
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            RoleTitle(role));
    }

    private async Task<TelegramUserOverviewResult> BuildUserCardAsync(
        long userId,
        long actorUserId,
        CancellationToken cancellationToken,
        string? callbackAnswer = null)
    {
        var result = await _managementService.GetUserDetailsAsync(userId, cancellationToken);
        if (result.User is null)
        {
            return Stale();
        }

        var user = result.User;
        var hasPrivateChat = user.TelegramChatId > 0;
        var reachable = hasPrivateChat && user.IsEnabled && !user.IsBlocked;
        var text =
            "👤 Пользователь\n\n" +
            $"Имя: {DisplayName(user)}\n" +
            $"Username: {(string.IsNullOrWhiteSpace(user.Username) ? "—" : $"@{user.Username.Trim().TrimStart('@')}")}\n" +
            $"TelegramId: {(user.TelegramUserId ?? user.TelegramChatId).ToString(CultureInfo.InvariantCulture)}\n" +
            $"Текущая роль: {RoleTitle(user.Role)}\n" +
            $"Статус: {StatusLabel(user)}\n" +
            $"Личный чат: {(hasPrivateChat ? "есть" : "нет")}\n" +
            $"Доступен для рассылки: {YesNo(reachable)}\n" +
            $"Последняя активность: {ActivityLabel(user.LastSeenAt)}";

        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>();
        if (user.Id != actorUserId)
        {
            rows.Add([Button("🔃 Изменить уровень", $"usr:role:{user.Id}")]);
            rows.Add(
            [
                user.IsBlocked
                    ? Button("✅ Разблокировать", $"usr:unblock:{user.Id}")
                    : Button("🚫 Заблокировать", $"usr:block:{user.Id}")
            ]);
        }
        rows.Add([Button("Назад к роли", RoleCallback(user.Role, 0))]);
        rows.Add([Button("Назад к пользователям", UsersOverviewCallback)]);

        return new(
            text,
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            callbackAnswer);
    }

    private async Task<TelegramUserOverviewResult> BuildRolePickerAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var result = await _managementService.GetUserDetailsAsync(userId, cancellationToken);
        if (result.User is null)
        {
            return Stale();
        }

        var rows = Enum.GetValues<TelegramUserRole>()
            .Select(role => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [Button(RoleTitle(role), $"usr:set:{userId}:{RoleSlug(role)}")])
            .Append([Button("Назад", $"usr:view:{userId}")])
            .ToArray();
        return new(
            "Выберите новый уровень:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Выберите уровень");
    }

    private async Task<TelegramUserOverviewResult> BuildRoleConfirmationAsync(
        long userId,
        TelegramUserRole role,
        CancellationToken cancellationToken)
    {
        var result = await _managementService.GetUserDetailsAsync(userId, cancellationToken);
        if (result.User is null)
        {
            return Stale();
        }

        return new(
            $"Назначить роль {RoleTitle(role)} пользователю {UserLabel(result.User)}?",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
            [
                [Button("Да", $"usr:confirm:set:{userId}:{RoleSlug(role)}")],
                [Button("Отмена", $"usr:view:{userId}")]
            ]),
            "Подтвердите роль");
    }

    private async Task<TelegramUserOverviewResult> BuildBlockConfirmationAsync(
        long userId,
        bool block,
        CancellationToken cancellationToken)
    {
        var result = await _managementService.GetUserDetailsAsync(userId, cancellationToken);
        if (result.User is null)
        {
            return Stale();
        }

        var action = block ? "блокировку" : "разблокировку";
        var confirmAction = block ? "block" : "unblock";
        var confirmText = block ? "Да, заблокировать" : "Да, разблокировать";
        return new(
            $"Подтвердить {action} пользователя {UserLabel(result.User)}?",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
            [
                [Button(confirmText, $"usr:confirm:{confirmAction}:{userId}")],
                [Button("Отмена", $"usr:view:{userId}")]
            ]),
            "Требуется подтверждение");
    }

    private async Task<TelegramUserOverviewResult> MutationResultAsync(
        TelegramUserManagementResult result,
        long actorUserId,
        string successAnswer,
        CancellationToken cancellationToken)
    {
        if (result.Status == TelegramUserManagementStatus.Success && result.User is not null)
        {
            return await BuildUserCardAsync(
                result.User.Id,
                actorUserId,
                cancellationToken,
                successAnswer);
        }

        var text = result.Status switch
        {
            TelegramUserManagementStatus.CannotModifySelf =>
                "Нельзя изменить собственный уровень или заблокировать себя.",
            TelegramUserManagementStatus.CannotModifyLastOwner =>
                "Нельзя понизить или заблокировать последнего Owner.",
            TelegramUserManagementStatus.AlreadyBlocked => "Пользователь уже заблокирован.",
            TelegramUserManagementStatus.AlreadyActive => "Пользователь уже активен.",
            TelegramUserManagementStatus.InvalidRole => "Выбран недопустимый уровень.",
            TelegramUserManagementStatus.AccessDenied => "Раздел пользователей доступен только владельцу.",
            _ => "Пользователь не найден или действие устарело."
        };
        return new(text, CallbackAnswerText: "Действие не выполнено");
    }

    private static bool IsOwner(TelegramUserAccessResult access) =>
        access.User is { IsEnabled: true, IsBlocked: false, Role: TelegramUserRole.Owner };

    private static TelegramUserOverviewResult Denied() =>
        new(
            "Раздел пользователей доступен только владельцу.",
            CallbackAnswerText: "Нет доступа");

    private static TelegramUserOverviewResult Stale() =>
        new(
            "Список пользователей устарел.",
            CallbackAnswerText: "Устарело");

    private static string RoleCallback(TelegramUserRole role, int page) =>
        $"{RolePrefix}{RoleSlug(role)}:{Math.Max(0, page)}";

    private static bool TryParseRolePage(
        string callbackData,
        out TelegramUserRole role,
        out int page)
    {
        role = TelegramUserRole.Consumer;
        page = 0;
        if (!callbackData.StartsWith(RolePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = callbackData[RolePrefix.Length..].Split(':', 2);
        if (parts.Length != 2 ||
            !TryParseRole(parts[0], out role) ||
            !int.TryParse(parts[1], out page))
        {
            return false;
        }

        page = Math.Max(0, page);
        return true;
    }

    private static bool TryParseUserAction(
        string callbackData,
        string action,
        out long userId)
    {
        userId = 0;
        var prefix = $"usr:{action}:";
        return callbackData.StartsWith(prefix, StringComparison.Ordinal) &&
            long.TryParse(callbackData[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out userId) &&
            userId > 0;
    }

    private static bool TryParseRoleAction(
        string callbackData,
        string action,
        out long userId,
        out TelegramUserRole role)
    {
        userId = 0;
        role = TelegramUserRole.Consumer;
        var prefix = $"usr:{action}:";
        if (!callbackData.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = callbackData[prefix.Length..].Split(':', 2);
        return parts.Length == 2 &&
            long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out userId) &&
            userId > 0 &&
            TryParseRole(parts[1], out role);
    }

    private static bool TryParseRole(string value, out TelegramUserRole role)
    {
        role = value switch
        {
            "owner" => TelegramUserRole.Owner,
            "admin" => TelegramUserRole.Admin,
            "eng" => TelegramUserRole.Engineer,
            "inst" => TelegramUserRole.Installer,
            "cons" => TelegramUserRole.Consumer,
            _ => TelegramUserRole.Consumer
        };
        return value is "owner" or "admin" or "eng" or "inst" or "cons";
    }

    private static string RoleSlug(TelegramUserRole role) =>
        role switch
        {
            TelegramUserRole.Owner => "owner",
            TelegramUserRole.Admin => "admin",
            TelegramUserRole.Engineer => "eng",
            TelegramUserRole.Installer => "inst",
            _ => "cons"
        };

    private static string RoleTitle(TelegramUserRole role) => role.ToString();

    private static string UserLabel(TelegramUserListItem user) =>
        UserLabel(user.FirstName, user.LastName, user.Username);

    private static string UserLabel(TelegramUserSnapshot user) =>
        UserLabel(user.FirstName, user.LastName, user.Username);

    private static string UserLabel(
        string? firstName,
        string? lastName,
        string? username)
    {
        var name = string.Join(
            ' ',
            new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "User";
        }

        return string.IsNullOrWhiteSpace(username)
            ? name
            : $"{name} (@{username.Trim().TrimStart('@')})";
    }

    private static string ShortUserLabel(TelegramUserListItem user)
    {
        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            return user.FirstName.Trim();
        }
        if (!string.IsNullOrWhiteSpace(user.Username))
        {
            return $"@{user.Username.Trim().TrimStart('@')}";
        }
        return "User";
    }

    private static string DisplayName(TelegramUserSnapshot user)
    {
        var name = string.Join(
            ' ',
            new[] { user.FirstName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
        return string.IsNullOrWhiteSpace(name) ? "User" : name;
    }

    private static string StatusLabel(TelegramUserListItem user) =>
        user.IsBlocked ? "Заблокирован" : user.IsEnabled ? "Активен" : "Неактивен";

    private static string StatusLabel(TelegramUserSnapshot user) =>
        user.IsBlocked ? "Заблокирован" : user.IsEnabled ? "Активен" : "Неактивен";

    private static string ActivityLabel(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("dd.MM.yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture) ?? "—";

    private static string YesNo(bool value) => value ? "да" : "нет";

    private static EquipmentDiagnosticTelegramInlineKeyboardButton Button(
        string text,
        string callbackData) =>
        new(text, callbackData);
}

public sealed record TelegramUserOverviewResult(
    string Text,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    string? CallbackAnswerText = null);
