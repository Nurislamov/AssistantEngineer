using System.Text;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public sealed class TelegramUserOverviewService
{
    public const string UsersButton = "👥 Пользователи";
    public const string UsersOverviewCallback = "usr:stats";
    private const string RolePrefix = "usr:r:";
    private const int PageSize = 10;

    private readonly ITelegramUserStore _userStore;

    public TelegramUserOverviewService(ITelegramUserStore userStore)
    {
        _userStore = userStore;
    }

    public static bool IsCallback(string? callbackData) =>
        string.Equals(callbackData, UsersOverviewCallback, StringComparison.Ordinal) ||
        callbackData?.StartsWith(RolePrefix, StringComparison.Ordinal) == true;

    public async Task<TelegramUserOverviewResult> HandleCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!IsOwner(access))
        {
            return new TelegramUserOverviewResult(
                "Раздел пользователей доступен только владельцу.",
                CallbackAnswerText: "Нет доступа");
        }

        if (string.Equals(update.CallbackData, UsersOverviewCallback, StringComparison.Ordinal))
        {
            return await BuildOverviewAsync(cancellationToken);
        }

        return TryParseRolePage(update.CallbackData, out var role, out var page)
            ? await BuildRolePageAsync(role, page, cancellationToken)
            : new TelegramUserOverviewResult("Список пользователей устарел.", CallbackAnswerText: "Устарело");
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
            builder.AppendLine($"{RoleSlugTitle(role)}: {overview.CountsByRole.GetValueOrDefault(role)}");
        }

        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>();
        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("📊 Обновить", UsersOverviewCallback)]);
        foreach (var role in Enum.GetValues<TelegramUserRole>())
        {
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton(RoleSlugTitle(role), RoleCallback(role, 0))]);
        }
        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", "lib:open")]);

        return new TelegramUserOverviewResult(
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
        builder.AppendLine($"👥 Пользователи — {RoleSlugTitle(role)}");
        builder.AppendLine();
        builder.AppendLine($"Всего: {result.TotalCount}");
        builder.AppendLine($"Страница {result.Page + 1}/{result.TotalPages}");
        builder.AppendLine();

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
                builder.AppendLine($"   TelegramId: {TelegramIdLabel(user)}");
                builder.AppendLine($"   Личный чат: {YesNo(user.HasPrivateChat)}");
                builder.AppendLine($"   Для рассылки: {YesNo(user.IsReachableForPrivateMessage)}");
                if (index + 1 < result.Users.Count)
                {
                    builder.AppendLine();
                }
            }
        }

        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>();
        var navigation = new List<EquipmentDiagnosticTelegramInlineKeyboardButton>();
        if (result.Page > 0)
        {
            navigation.Add(new EquipmentDiagnosticTelegramInlineKeyboardButton("⬅️ Назад", RoleCallback(role, result.Page - 1)));
        }
        if (result.Page + 1 < result.TotalPages)
        {
            navigation.Add(new EquipmentDiagnosticTelegramInlineKeyboardButton("➡️ Далее", RoleCallback(role, result.Page + 1)));
        }
        if (navigation.Count > 0)
        {
            rows.Add(navigation);
        }
        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад к пользователям", UsersOverviewCallback)]);

        return new TelegramUserOverviewResult(
            builder.ToString().TrimEnd(),
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            RoleSlugTitle(role));
    }

    private static bool IsOwner(TelegramUserAccessResult access) =>
        access.User is { IsEnabled: true, IsBlocked: false, Role: TelegramUserRole.Owner };

    private static string RoleCallback(TelegramUserRole role, int page) =>
        $"{RolePrefix}{RoleSlug(role)}:{Math.Max(0, page)}";

    private static bool TryParseRolePage(
        string? callbackData,
        out TelegramUserRole role,
        out int page)
    {
        role = TelegramUserRole.Consumer;
        page = 0;
        if (callbackData?.StartsWith(RolePrefix, StringComparison.Ordinal) != true)
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

    private static string RoleSlugTitle(TelegramUserRole role) =>
        role.ToString();

    private static string UserLabel(TelegramUserListItem user)
    {
        var name = string.Join(
            ' ',
            new[] { user.FirstName, user.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!.Trim()));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "User";
        }

        return string.IsNullOrWhiteSpace(user.Username)
            ? name
            : $"{name} (@{user.Username.Trim().TrimStart('@')})";
    }

    private static string TelegramIdLabel(TelegramUserListItem user) =>
        user.TelegramAccountId?.ToString(System.Globalization.CultureInfo.InvariantCulture) ??
        user.TelegramChatId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string YesNo(bool value) => value ? "да" : "нет";
}

public sealed record TelegramUserOverviewResult(
    string Text,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    string? CallbackAnswerText = null);
