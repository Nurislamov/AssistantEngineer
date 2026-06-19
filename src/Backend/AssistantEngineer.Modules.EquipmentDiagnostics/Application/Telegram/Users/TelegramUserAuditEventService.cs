using System.Text;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public sealed class TelegramUserAuditEventService
{
    private const int AuditLimit = 10;

    private readonly ITelegramUserAuditEventStore _store;
    private readonly ITelegramUserStore _userStore;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;
    private readonly ILogger<TelegramUserAuditEventService> _logger;

    public TelegramUserAuditEventService(
        ITelegramUserAuditEventStore store,
        ITelegramUserStore userStore,
        TelegramDisplayTimeFormatter timeFormatter,
        ILogger<TelegramUserAuditEventService>? logger = null)
    {
        _store = store;
        _userStore = userStore;
        _timeFormatter = timeFormatter;
        _logger = logger ?? NullLogger<TelegramUserAuditEventService>.Instance;
    }

    public async Task AppendSafeAsync(
        TelegramUserAuditEventCreate request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _store.AppendAsync(request with
            {
                Message = SafeMessage(request.EventType),
                MetadataJson = SafeMetadata(request.MetadataJson)
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram user audit event write failed; primary action remains committed. EventType: {EventType}; ExceptionType: {ExceptionType}.",
                request.EventType,
                exception.GetType().Name);
        }
    }

    public async Task<string> FormatLatestAsync(CancellationToken cancellationToken = default)
    {
        var events = await _store.GetLatestAsync(AuditLimit, cancellationToken);
        if (events.Count == 0)
        {
            return "Аудит управления пользователями\n\nСобытий пока нет.";
        }

        var builder = new StringBuilder("Аудит управления пользователями\n");
        foreach (var item in events)
        {
            builder.AppendLine();
            builder.Append($"{_timeFormatter.FormatAbsolute(item.CreatedAt)} — ");
            builder.Append(await FormatEventAsync(item, cancellationToken));
        }
        return builder.ToString();
    }

    private async Task<string> FormatEventAsync(
        TelegramUserAuditEventSnapshot item,
        CancellationToken cancellationToken)
    {
        var actor = await UserLabelAsync(item.ActorTelegramUserId, cancellationToken);
        var target = await UserLabelAsync(item.TargetTelegramUserId, cancellationToken);
        return item.EventType switch
        {
            TelegramUserAuditEventType.RoleChanged =>
                $"{actor}: роль {target} изменена {RoleLabel(item.OldRole)} → {RoleLabel(item.NewRole)}",
            TelegramUserAuditEventType.UserEnabled => $"{actor}: доступ {target} включён",
            TelegramUserAuditEventType.UserDisabled => $"{actor}: доступ {target} отключён",
            TelegramUserAuditEventType.UserBlocked => $"{actor}: {target} заблокирован",
            TelegramUserAuditEventType.UserUnblocked => $"{actor}: {target} разблокирован",
            TelegramUserAuditEventType.UserActionDenied => $"{actor}: действие для {target} отклонено",
            _ => "событие управления пользователем"
        };
    }

    private async Task<string> UserLabelAsync(long? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return "система";
        }

        var user = await _userStore.GetByIdAsync(id.Value, cancellationToken);
        if (user is null)
        {
            return "пользователь";
        }
        if (!string.IsNullOrWhiteSpace(user.Username))
        {
            return $"@{user.Username.Trim().TrimStart('@')}";
        }
        return "пользователь";
    }

    private static string RoleLabel(TelegramUserRole? role) => role?.ToString() ?? "—";

    private static string SafeMessage(TelegramUserAuditEventType type) =>
        type switch
        {
            TelegramUserAuditEventType.RoleChanged => "Telegram user role changed.",
            TelegramUserAuditEventType.UserEnabled => "Telegram user enabled.",
            TelegramUserAuditEventType.UserDisabled => "Telegram user disabled.",
            TelegramUserAuditEventType.UserBlocked => "Telegram user blocked.",
            TelegramUserAuditEventType.UserUnblocked => "Telegram user unblocked.",
            TelegramUserAuditEventType.UserActionDenied => "Telegram user management action denied.",
            _ => "Telegram user management event."
        };

    private static string? SafeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            var action = document.RootElement.TryGetProperty("action", out var actionElement)
                ? actionElement.GetString()
                : null;
            var reason = document.RootElement.TryGetProperty("reason", out var reasonElement)
                ? reasonElement.GetString()
                : null;
            if (!AllowedActions.Contains(action) ||
                reason is not null && !AllowedReasons.Contains(reason))
            {
                return null;
            }

            return reason is null
                ? JsonSerializer.Serialize(new { action })
                : JsonSerializer.Serialize(new { action, reason });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static readonly HashSet<string?> AllowedActions =
    [
        "role",
        "enable",
        "disable",
        "block",
        "unblock",
        "unsupported"
    ];

    private static readonly HashSet<string?> AllowedReasons =
    [
        "owner_protected",
        "self_action_denied",
        "insufficient_permissions",
        "target_not_found",
        "invalid_role",
        "disabled_or_blocked_actor",
        "unsupported_action"
    ];
}
