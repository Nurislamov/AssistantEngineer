using System.Text;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public sealed class TelegramServiceRequestEventService
{
    private const int HistoryLimit = 50;

    private readonly ITelegramServiceRequestEventStore _store;
    private readonly ITelegramUserStore _userStore;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;
    private readonly ILogger<TelegramServiceRequestEventService> _logger;

    public TelegramServiceRequestEventService(
        ITelegramServiceRequestEventStore store,
        ITelegramUserStore userStore,
        TelegramDisplayTimeFormatter timeFormatter,
        ILogger<TelegramServiceRequestEventService>? logger = null)
    {
        _store = store;
        _userStore = userStore;
        _timeFormatter = timeFormatter;
        _logger = logger ?? NullLogger<TelegramServiceRequestEventService>.Instance;
    }

    public async Task AppendSafeAsync(
        TelegramServiceRequestEventCreate request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _store.AppendAsync(request with
            {
                Message = SafeMessage(request.EventType),
                MetadataJson = SafeMetadata(request.EventType, request.MetadataJson)
            }, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram service request audit event write failed; primary action remains committed. RequestId: {RequestId}; EventType: {EventType}; ExceptionType: {ExceptionType}.",
                request.ServiceRequestId,
                request.EventType,
                exception.GetType().Name);
        }
    }

    public async Task<string> FormatHistoryAsync(
        long serviceRequestId,
        CancellationToken cancellationToken = default)
    {
        var events = await _store.GetLatestAsync(serviceRequestId, HistoryLimit, cancellationToken);
        if (events.Count == 0)
        {
            return $"История заявки #{serviceRequestId}\n\nИстория заявки пока пустая.";
        }

        var builder = new StringBuilder($"История заявки #{serviceRequestId}\n");
        foreach (var item in events)
        {
            builder.AppendLine();
            builder.Append($"{_timeFormatter.FormatAbsolute(item.CreatedAt)} — ");
            builder.Append(await FormatEventAsync(item, cancellationToken));
        }
        return builder.ToString();
    }

    private async Task<string> FormatEventAsync(
        TelegramServiceRequestEventSnapshot item,
        CancellationToken cancellationToken)
    {
        var actor = await UserLabelAsync(item.ActorTelegramUserId, cancellationToken);
        var target = await UserLabelAsync(item.TargetTelegramUserId, cancellationToken);
        return item.EventType switch
        {
            TelegramServiceRequestEventType.Created => $"создана клиентом {actor}",
            TelegramServiceRequestEventType.NotificationSent => "уведомление отправлено в сервисную группу",
            TelegramServiceRequestEventType.NotificationFailed => "не удалось отправить уведомление в сервисную группу",
            TelegramServiceRequestEventType.Taken => $"взята в работу {actor}",
            TelegramServiceRequestEventType.Assigned => $"назначена инженеру {target} администратором {actor}",
            TelegramServiceRequestEventType.Reassigned => $"переназначена инженеру {target} администратором {actor}",
            TelegramServiceRequestEventType.ContactRequested => $"контакт запрошен пользователем {actor}",
            TelegramServiceRequestEventType.ContactSent => $"контакт отправлен {target} в личный чат",
            TelegramServiceRequestEventType.ContactFailed => $"не удалось отправить контакт {target} в личный чат",
            TelegramServiceRequestEventType.ContactDenied => $"доступ к контакту отклонён для пользователя {actor}",
            TelegramServiceRequestEventType.HistoryViewed => $"история открыта пользователем {actor}",
            TelegramServiceRequestEventType.HistoryDenied => $"доступ к истории отклонён для пользователя {actor}",
            TelegramServiceRequestEventType.ActionDenied => "действие с заявкой отклонено",
            TelegramServiceRequestEventType.Resolved => $"закрыта пользователем {actor}",
            TelegramServiceRequestEventType.Cancelled => $"отменена пользователем {actor}",
            TelegramServiceRequestEventType.CustomerNotificationSent => "клиенту отправлено уведомление о статусе",
            TelegramServiceRequestEventType.CustomerNotificationFailed => "не удалось отправить клиенту уведомление о статусе",
            _ => "событие заявки"
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
        var name = string.Join(
            " ",
            new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? "пользователь" : name;
    }

    private static string SafeMessage(TelegramServiceRequestEventType type) =>
        type switch
        {
            TelegramServiceRequestEventType.Created => "Service request created.",
            TelegramServiceRequestEventType.NotificationSent => "Service group notification sent.",
            TelegramServiceRequestEventType.NotificationFailed => "Service group notification failed.",
            TelegramServiceRequestEventType.Taken => "Service request taken.",
            TelegramServiceRequestEventType.Assigned => "Service request assigned.",
            TelegramServiceRequestEventType.Reassigned => "Service request reassigned.",
            TelegramServiceRequestEventType.ContactRequested => "Private contact requested.",
            TelegramServiceRequestEventType.ContactSent => "Private contact delivered.",
            TelegramServiceRequestEventType.ContactFailed => "Private contact delivery failed.",
            TelegramServiceRequestEventType.ContactDenied => "Private contact access denied.",
            TelegramServiceRequestEventType.HistoryViewed => "Service request history viewed.",
            TelegramServiceRequestEventType.HistoryDenied => "Service request history access denied.",
            TelegramServiceRequestEventType.ActionDenied => "Service request action denied.",
            TelegramServiceRequestEventType.Resolved => "Service request resolved.",
            TelegramServiceRequestEventType.Cancelled => "Service request cancelled.",
            TelegramServiceRequestEventType.CustomerNotificationSent => "Customer notification delivered.",
            TelegramServiceRequestEventType.CustomerNotificationFailed => "Customer notification delivery failed.",
            _ => "Service request event."
        };

    private static string? SafeMetadata(
        TelegramServiceRequestEventType eventType,
        string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);
            if (eventType == TelegramServiceRequestEventType.ContactSent &&
                document.RootElement.TryGetProperty("contact_delivered", out var delivered) &&
                delivered.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return JsonSerializer.Serialize(new
                {
                    contact_delivered = delivered.GetBoolean()
                });
            }

            if (eventType == TelegramServiceRequestEventType.ActionDenied &&
                document.RootElement.TryGetProperty("action", out var actionElement) &&
                document.RootElement.TryGetProperty("reason", out var reasonElement))
            {
                var action = actionElement.GetString();
                var reason = reasonElement.GetString();
                if (AllowedActions.Contains(action) && AllowedReasons.Contains(reason))
                {
                    return JsonSerializer.Serialize(new { action, reason });
                }
            }
        }
        catch (JsonException)
        {
            // Invalid or untrusted metadata is deliberately discarded.
        }

        return null;
    }

    private static readonly HashSet<string?> AllowedActions =
    [
        "take",
        "assign",
        "close",
        "cancel",
        "contact",
        "history",
        "status"
    ];

    private static readonly HashSet<string?> AllowedReasons =
    [
        "forbidden",
        "terminal_status",
        "assigned_to_another_engineer",
        "already_assigned",
        "not_found"
    ];
}
