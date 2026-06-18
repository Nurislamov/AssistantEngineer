using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public enum TelegramServiceQueueCommandKind
{
    Queue,
    Take,
    Assign,
    Done,
    Cancel,
    Status,
    Contact
}

public sealed record TelegramServiceQueueCommand(
    TelegramServiceQueueCommandKind Kind,
    long? RequestId,
    string? Username);

public sealed record TelegramServiceQueueCommandResult(
    string Text,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    string? CallbackAnswerText = null,
    bool SuppressGroupMessage = false);

public sealed class TelegramServiceRequestQueueService
{
    private const int QueueLimit = 10;
    private const int QueueButtonLimit = 5;

    private readonly ITelegramServiceRequestStore _requestStore;
    private readonly ITelegramUserStore _userStore;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;
    private readonly TelegramServiceRequestCardRenderer _cardRenderer;
    private readonly ILogger<TelegramServiceRequestQueueService> _logger;

    public TelegramServiceRequestQueueService(
        ITelegramServiceRequestStore requestStore,
        ITelegramUserStore userStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        EquipmentDiagnosticTelegramOptions options,
        TelegramDisplayTimeFormatter timeFormatter,
        TelegramServiceRequestCardRenderer cardRenderer,
        ILogger<TelegramServiceRequestQueueService>? logger = null)
    {
        _requestStore = requestStore;
        _userStore = userStore;
        _outboundClient = outboundClient;
        _options = options;
        _timeFormatter = timeFormatter;
        _cardRenderer = cardRenderer;
        _logger = logger ?? NullLogger<TelegramServiceRequestQueueService>.Instance;
    }

    public static bool TryParse(string? text, out TelegramServiceQueueCommand command)
    {
        command = null!;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var commandName = parts[0].Split('@', 2)[0].ToLowerInvariant();
        var kind = commandName switch
        {
            "/queue" => TelegramServiceQueueCommandKind.Queue,
            "/take" => TelegramServiceQueueCommandKind.Take,
            "/assign" => TelegramServiceQueueCommandKind.Assign,
            "/done" => TelegramServiceQueueCommandKind.Done,
            "/cancel_request" => TelegramServiceQueueCommandKind.Cancel,
            "/request_status" => TelegramServiceQueueCommandKind.Status,
            "/contact" => TelegramServiceQueueCommandKind.Contact,
            _ => (TelegramServiceQueueCommandKind?)null
        };
        if (kind is null)
        {
            return false;
        }

        long? requestId = null;
        if (kind != TelegramServiceQueueCommandKind.Queue &&
            (parts.Length < 2 || !long.TryParse(parts[1].TrimStart('#'), out var parsedId) || parsedId <= 0))
        {
            command = new TelegramServiceQueueCommand(kind.Value, null, null);
            return true;
        }
        if (parts.Length >= 2 && long.TryParse(parts[1].TrimStart('#'), out var id))
        {
            requestId = id;
        }

        var username = kind == TelegramServiceQueueCommandKind.Assign && parts.Length >= 3
            ? parts[2].Trim().TrimStart('@')
            : null;
        command = new TelegramServiceQueueCommand(kind.Value, requestId, username);
        return true;
    }

    public async Task<TelegramServiceQueueCommandResult> HandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramServiceQueueCommand command,
        CancellationToken cancellationToken = default)
    {
        if (_options.ServiceRequests.NotificationChatId is null ||
            update.ChatId != _options.ServiceRequests.NotificationChatId.Value)
        {
            return Result("Команда доступна в сервисной группе.");
        }

        var actor = update.UserId is null
            ? null
            : await _userStore.GetByTelegramUserIdAsync(update.UserId.Value, cancellationToken);
        if (actor is null)
        {
            return Result("Команда недоступна. Сначала откройте бота в личке и нажмите /start.");
        }
        if (!actor.IsEnabled || actor.IsBlocked)
        {
            return Result("Команда недоступна.");
        }
        if (actor.Role is not (TelegramUserRole.Owner or TelegramUserRole.Admin or TelegramUserRole.Engineer))
        {
            return Result("Команда недоступна.");
        }

        return command.Kind switch
        {
            TelegramServiceQueueCommandKind.Queue => await FormatQueueResultAsync(cancellationToken),
            TelegramServiceQueueCommandKind.Take => await TakeAsync(command, actor, cancellationToken),
            TelegramServiceQueueCommandKind.Assign => await AssignAsync(command, actor, cancellationToken),
            TelegramServiceQueueCommandKind.Done => await CloseAsync(command, actor, TelegramServiceRequestStatus.Resolved, cancellationToken),
            TelegramServiceQueueCommandKind.Cancel => await CloseAsync(command, actor, TelegramServiceRequestStatus.Cancelled, cancellationToken),
            TelegramServiceQueueCommandKind.Status => await StatusAsync(command, cancellationToken),
            TelegramServiceQueueCommandKind.Contact => await ContactAsync(command, actor, cancellationToken),
            _ => Result("Команда недоступна.")
        };
    }

    public async Task<TelegramServiceQueueCommandResult> HandleCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (_options.ServiceRequests.NotificationChatId is null ||
            update.ChatId != _options.ServiceRequests.NotificationChatId.Value)
        {
            return CallbackResult("Действие доступно в сервисной группе.", "Нет доступа");
        }

        if (!TryParseCallbackData(update.CallbackData, out var action, out var requestId, out var targetUserId))
        {
            return CallbackResult("Действие недоступно или устарело.", "Ошибка действия");
        }

        var actor = update.UserId is null
            ? null
            : await _userStore.GetByTelegramUserIdAsync(update.UserId.Value, cancellationToken);
        if (actor is null)
        {
            return CallbackResult("Действие недоступно. Сначала откройте бота в личке и нажмите /start.", "Нет доступа");
        }
        if (!actor.IsEnabled || actor.IsBlocked)
        {
            return CallbackResult("Действие недоступно.", "Нет доступа");
        }
        if (actor.Role is not (TelegramUserRole.Owner or TelegramUserRole.Admin or TelegramUserRole.Engineer))
        {
            return CallbackResult(action is "a" or "as"
                ? "Назначать инженера может только Owner или Admin."
                : "Действие недоступно.", "Нет доступа");
        }

        var result = action switch
        {
            "t" => await TakeAsync(new TelegramServiceQueueCommand(TelegramServiceQueueCommandKind.Take, requestId, null), actor, cancellationToken),
            "a" => await AssignMenuAsync(requestId, actor, cancellationToken),
            "as" => await AssignSelectedAsync(requestId, targetUserId, actor, cancellationToken),
            "c" => await ContactAsync(new TelegramServiceQueueCommand(TelegramServiceQueueCommandKind.Contact, requestId, null), actor, cancellationToken),
            "d" => await CloseAsync(new TelegramServiceQueueCommand(TelegramServiceQueueCommandKind.Done, requestId, null), actor, TelegramServiceRequestStatus.Resolved, cancellationToken),
            "x" => await CloseAsync(new TelegramServiceQueueCommand(TelegramServiceQueueCommandKind.Cancel, requestId, null), actor, TelegramServiceRequestStatus.Cancelled, cancellationToken),
            "s" => await StatusAsync(new TelegramServiceQueueCommand(TelegramServiceQueueCommandKind.Status, requestId, null), cancellationToken),
            "b" => await BackToCardAsync(requestId, cancellationToken),
            _ => Result("Действие недоступно или устарело.")
        };

        if (action == "a" && result.ReplyMarkup is not null)
        {
            await EditAssignmentMenuAsync(requestId, result, cancellationToken);
        }
        else if (action == "s")
        {
            await RefreshRequestCardAsync(requestId, cancellationToken);
        }

        var answer = action switch
        {
            "s" => "Статус обновлён",
            "c" => result.Text.Contains("отправлен", StringComparison.OrdinalIgnoreCase)
                ? "Контакт отправлен в личный чат"
                : result.Text.Contains("Откройте", StringComparison.OrdinalIgnoreCase)
                    ? "Откройте личный чат с ботом и нажмите /start"
                    : "Контакт недоступен",
            _ when result.Text.Contains("недоступ", StringComparison.OrdinalIgnoreCase) ||
                result.Text.Contains("только", StringComparison.OrdinalIgnoreCase) ||
                result.Text.Contains("не найден", StringComparison.OrdinalIgnoreCase) => "Ошибка действия",
            _ => "Готово"
        };
        return result with { CallbackAnswerText = answer, SuppressGroupMessage = true };
    }

    public static EquipmentDiagnosticTelegramReplyMarkup NewRequestKeyboard(long requestId) =>
        InlineKeyboard(
        [
            [Button("Взять в работу", Callback("t", requestId)), Button("Назначить", Callback("a", requestId))],
            [Button("Контакт", Callback("c", requestId)), Button("Статус", Callback("s", requestId))],
            [Button("Отменить", Callback("x", requestId))]
        ]);

    private async Task<TelegramServiceQueueCommandResult> FormatQueueResultAsync(
        CancellationToken cancellationToken)
    {
        var requests = await _requestStore.GetActiveAsync(QueueLimit, cancellationToken);
        if (requests.Count == 0)
        {
            return Result("Активных сервисных заявок нет.");
        }

        var text = await FormatQueueAsync(requests, cancellationToken);
        var rows = requests
            .Take(QueueButtonLimit)
            .Select(request => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
            [
                Button($"#{request.Id} Статус", Callback("s", request.Id)),
                Button($"#{request.Id} Взять", Callback("t", request.Id)),
                Button($"#{request.Id} Назначить", Callback("a", request.Id))
            ])
            .ToArray();
        return Result(text, InlineKeyboard(rows));
    }

    private async Task<string> FormatQueueAsync(
        IReadOnlyList<TelegramServiceRequestSnapshot> requests,
        CancellationToken cancellationToken)
    {
        var builder = new StringBuilder("Сервисные заявки\n\n");
        foreach (var request in requests)
        {
            var assignee = await AssigneeLabelAsync(request.AssignedTelegramUserId, cancellationToken);
            builder.Append($"#{request.Id} — {Title(request)} — {TelegramServiceRequestService.StatusLabel(request.Status)}");
            if (assignee is not null)
            {
                builder.Append($" — {assignee}");
            }
            builder.Append($" — {_timeFormatter.FormatRelative(request.CreatedAt)} — Телефон: ");
            builder.AppendLine(request.PhoneWasSaved ? "сохранён" : "не сохранён");
        }

        return builder.ToString().TrimEnd();
    }

    private async Task<TelegramServiceQueueCommandResult> AssignMenuAsync(
        long requestId,
        TelegramUserSnapshot actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role is not (TelegramUserRole.Owner or TelegramUserRole.Admin))
        {
            return Result("Назначать инженера может только Owner или Admin.");
        }
        var request = await _requestStore.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return NotFound(requestId);
        }
        if (request.Status is TelegramServiceRequestStatus.Resolved or TelegramServiceRequestStatus.Cancelled)
        {
            return Result($"Заявка #{request.Id}: {TelegramServiceRequestService.StatusLabel(request.Status)}. Повторное открытие не поддерживается.");
        }

        var users = await _userStore.ListUsersAsync(100, cancellationToken);
        var candidates = users
            .Where(user => user.IsEnabled && !user.IsBlocked)
            .Where(user => user.Role is TelegramUserRole.Engineer or TelegramUserRole.Admin or TelegramUserRole.Owner)
            .OrderBy(UserDisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (candidates.Length == 0)
        {
            return Result("Нет доступных инженеров. Попросите инженера открыть бота и нажать /start, затем назначьте роль Engineer.");
        }

        var rows = candidates
            .Select(user => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
            [
                Button(UserDisplayName(user), CallbackAssign(requestId, user.Id))
            ])
            .Append(
            [
                Button("Назад", Callback("b", requestId))
            ])
            .ToArray();
        var assignee = await AssigneeLabelAsync(request.AssignedTelegramUserId, cancellationToken) ?? "не назначен";
        return Result(
            $"Выберите инженера для заявки #{requestId}:\n\n" +
            $"Ошибка: {Title(request)}\n" +
            $"Текущий статус: {TelegramServiceRequestService.StatusLabel(request.Status)}\n" +
            $"Текущий инженер: {assignee}",
            InlineKeyboard(rows));
    }

    private async Task<TelegramServiceQueueCommandResult> AssignSelectedAsync(
        long requestId,
        long? targetUserId,
        TelegramUserSnapshot actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role is not (TelegramUserRole.Owner or TelegramUserRole.Admin))
        {
            return Result("Назначать инженера может только Owner или Admin.");
        }
        if (targetUserId is null)
        {
            return Result("Инженер не найден.");
        }

        var request = await _requestStore.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return NotFound(requestId);
        }
        if (request.Status is TelegramServiceRequestStatus.Resolved or TelegramServiceRequestStatus.Cancelled)
        {
            return Result($"Заявка #{request.Id}: {TelegramServiceRequestService.StatusLabel(request.Status)}. Повторное открытие не поддерживается.");
        }

        var target = await _userStore.GetByIdAsync(targetUserId.Value, cancellationToken);
        if (target is null || !target.IsEnabled || target.IsBlocked)
        {
            return Result("Инженер не найден. Попросите его открыть бота и нажать /start, затем назначьте роль Engineer.");
        }
        if (target.Role is not (TelegramUserRole.Engineer or TelegramUserRole.Admin or TelegramUserRole.Owner))
        {
            return Result("Пользователь найден, но не имеет роли Engineer.");
        }

        var updated = await AssignInternalAsync(request, target, actor, cancellationToken);
        var customerDelivered = await NotifyCustomerAsync(updated, cancellationToken);
        var contactDelivered = await SendContactPrivatelyAsync(updated, target, cancellationToken);
        _logger.LogInformation(
            "Telegram service request assigned by inline action. CustomerNotificationSent: {CustomerNotificationSent}; ContactSent: {ContactSent}.",
            customerDelivered,
            contactDelivered);
        await RefreshRequestCardAsync(updated.Id, cancellationToken);

        var text = $"Заявка #{updated.Id} назначена: {UserLabel(target)}.";
        if (!contactDelivered)
        {
            text += $"\nИнженер должен открыть личный чат с ботом и нажать /start, затем нажать «Контакт» или выполнить /contact {updated.Id}.";
        }
        return Result(text);
    }

    private async Task<TelegramServiceQueueCommandResult> TakeAsync(
        TelegramServiceQueueCommand command,
        TelegramUserSnapshot actor,
        CancellationToken cancellationToken)
    {
        if (command.RequestId is null)
        {
            return Result("Использование: /take <id>");
        }

        var request = await _requestStore.GetByIdAsync(command.RequestId.Value, cancellationToken);
        if (request is null)
        {
            return NotFound(command.RequestId.Value);
        }
        if (request.Status == TelegramServiceRequestStatus.InProgress)
        {
            if (request.AssignedTelegramUserId == actor.Id)
            {
                return Result($"Заявка #{request.Id} уже назначена на вас.");
            }

            var assignee = await AssigneeLabelAsync(request.AssignedTelegramUserId, cancellationToken) ?? "другого специалиста";
            return Result($"Заявка #{request.Id} уже в работе: {assignee}. Для переназначения используйте /assign.");
        }
        if (request.Status is TelegramServiceRequestStatus.Resolved or TelegramServiceRequestStatus.Cancelled)
        {
            return Result($"Заявка #{request.Id}: {TelegramServiceRequestService.StatusLabel(request.Status)}.");
        }

        var updated = await AssignInternalAsync(request, actor, actor, cancellationToken);
        var customerDelivered = await NotifyCustomerAsync(updated, cancellationToken);
        var contactDelivered = await SendContactPrivatelyAsync(updated, actor, cancellationToken);
        _logger.LogInformation(
            "Telegram service request taken. CustomerNotificationSent: {CustomerNotificationSent}; ContactSent: {ContactSent}.",
            customerDelivered,
            contactDelivered);
        await RefreshRequestCardAsync(updated.Id, cancellationToken);

        var text = $"Заявка #{updated.Id} взята в работу: {UserLabel(actor)}.";
        if (!contactDelivered)
        {
            text += "\nОткройте личный чат с ботом и нажмите /start, затем используйте /contact " + updated.Id + ".";
        }
        return Result(text);
    }

    private async Task<TelegramServiceQueueCommandResult> AssignAsync(
        TelegramServiceQueueCommand command,
        TelegramUserSnapshot actor,
        CancellationToken cancellationToken)
    {
        if (actor.Role is not (TelegramUserRole.Owner or TelegramUserRole.Admin))
        {
            return Result("Назначать инженера может только Owner или Admin.");
        }
        if (command.RequestId is null || string.IsNullOrWhiteSpace(command.Username))
        {
            return Result("Использование: /assign <id> @username");
        }

        var request = await _requestStore.GetByIdAsync(command.RequestId.Value, cancellationToken);
        if (request is null)
        {
            return NotFound(command.RequestId.Value);
        }
        if (request.Status is TelegramServiceRequestStatus.Resolved or TelegramServiceRequestStatus.Cancelled)
        {
            return Result($"Заявка #{request.Id}: {TelegramServiceRequestService.StatusLabel(request.Status)}. Повторное открытие не поддерживается.");
        }

        var target = await _userStore.GetByUsernameAsync(command.Username, cancellationToken);
        if (target is null || !target.IsEnabled || target.IsBlocked)
        {
            return Result("Инженер не найден. Попросите его открыть бота и нажать /start, затем назначьте роль Engineer.");
        }
        if (target.Role is not (TelegramUserRole.Engineer or TelegramUserRole.Admin or TelegramUserRole.Owner))
        {
            return Result("Пользователь найден, но не имеет роли Engineer.");
        }

        var updated = await AssignInternalAsync(request, target, actor, cancellationToken);
        var customerDelivered = await NotifyCustomerAsync(updated, cancellationToken);
        var contactDelivered = await SendContactPrivatelyAsync(updated, target, cancellationToken);
        _logger.LogInformation(
            "Telegram service request assigned. CustomerNotificationSent: {CustomerNotificationSent}; ContactSent: {ContactSent}.",
            customerDelivered,
            contactDelivered);
        await RefreshRequestCardAsync(updated.Id, cancellationToken);

        var text = $"Заявка #{updated.Id} назначена: {UserLabel(target)}.";
        if (!contactDelivered)
        {
            text += "\nНазначенному специалисту нужно открыть личный чат с ботом и использовать /contact " + updated.Id + ".";
        }
        return Result(text);
    }

    private async Task<TelegramServiceQueueCommandResult> CloseAsync(
        TelegramServiceQueueCommand command,
        TelegramUserSnapshot actor,
        TelegramServiceRequestStatus terminalStatus,
        CancellationToken cancellationToken)
    {
        if (command.RequestId is null)
        {
            return Result(terminalStatus == TelegramServiceRequestStatus.Resolved
                ? "Использование: /done <id>"
                : "Использование: /cancel_request <id>");
        }

        var request = await _requestStore.GetByIdAsync(command.RequestId.Value, cancellationToken);
        if (request is null)
        {
            return NotFound(command.RequestId.Value);
        }
        if (request.Status == TelegramServiceRequestStatus.Resolved)
        {
            return Result($"Заявка #{request.Id} уже закрыта.");
        }
        if (request.Status == TelegramServiceRequestStatus.Cancelled)
        {
            return Result($"Заявка #{request.Id} уже отменена.");
        }
        if (actor.Role == TelegramUserRole.Engineer && request.AssignedTelegramUserId != actor.Id)
        {
            return Result(request.Status == TelegramServiceRequestStatus.New
                ? $"Сначала возьмите заявку #{request.Id} в работу командой /take {request.Id}."
                : "Изменять статус может только назначенный инженер или администратор.");
        }

        var now = DateTimeOffset.UtcNow;
        var updated = await _requestStore.UpdateAsync(
            new TelegramServiceRequestUpdate(
                request.Id,
                terminalStatus,
                request.AssignedTelegramUserId,
                request.AssignedAt,
                request.AssignedByTelegramUserId,
                now,
                actor.Id,
                now),
            cancellationToken) ?? request;
        var delivered = await NotifyCustomerAsync(updated, cancellationToken);
        _logger.LogInformation(
            "Telegram service request status changed. Status: {Status}; CustomerNotificationSent: {CustomerNotificationSent}.",
            terminalStatus,
            delivered);
        await RefreshRequestCardAsync(updated.Id, cancellationToken);
        return Result(terminalStatus == TelegramServiceRequestStatus.Resolved
            ? $"Заявка #{request.Id} закрыта."
            : $"Заявка #{request.Id} отменена.");
    }

    private async Task<TelegramServiceQueueCommandResult> StatusAsync(
        TelegramServiceQueueCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestId is null)
        {
            return Result("Использование: /request_status <id>");
        }

        var request = await _requestStore.GetByIdAsync(command.RequestId.Value, cancellationToken);
        if (request is null)
        {
            return NotFound(command.RequestId.Value);
        }
        var assignee = await AssigneeLabelAsync(request.AssignedTelegramUserId, cancellationToken) ?? "не назначен";
        return Result(
            $"Заявка #{request.Id}\n" +
            $"Ошибка: {Title(request)}\n" +
            $"Статус: {TelegramServiceRequestService.StatusLabel(request.Status)}\n" +
            $"Инженер: {assignee}\n" +
            $"Создана: {_timeFormatter.FormatRelative(request.CreatedAt)}\n" +
            $"Телефон: {(request.PhoneWasSaved ? "сохранён" : "не сохранён")}");
    }

    private async Task<TelegramServiceQueueCommandResult> ContactAsync(
        TelegramServiceQueueCommand command,
        TelegramUserSnapshot actor,
        CancellationToken cancellationToken)
    {
        if (command.RequestId is null)
        {
            return Result("Использование: /contact <id>");
        }

        var request = await _requestStore.GetByIdAsync(command.RequestId.Value, cancellationToken);
        if (request is null)
        {
            return NotFound(command.RequestId.Value);
        }
        if (actor.Role == TelegramUserRole.Engineer && request.AssignedTelegramUserId != actor.Id)
        {
            return Result("Контакт доступен только назначенному инженеру или администратору.");
        }
        if (!request.PhoneWasSaved)
        {
            return Result("Номер телефона по заявке не сохранён.");
        }

        var delivered = await SendContactPrivatelyAsync(request, actor, cancellationToken);
        return Result(delivered
            ? "Контакт отправлен в личный чат."
            : "Откройте личный чат с ботом и нажмите /start, затем повторите команду.");
    }

    private async Task<TelegramServiceRequestSnapshot> AssignInternalAsync(
        TelegramServiceRequestSnapshot request,
        TelegramUserSnapshot assignee,
        TelegramUserSnapshot actor,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _requestStore.UpdateAsync(
            new TelegramServiceRequestUpdate(
                request.Id,
                TelegramServiceRequestStatus.InProgress,
                assignee.Id,
                now,
                actor.Id,
                now,
                actor.Id,
                ClosedAt: null),
            cancellationToken) ?? request;
    }

    private async Task<bool> NotifyCustomerAsync(
        TelegramServiceRequestSnapshot request,
        CancellationToken cancellationToken)
    {
        var customer = await _userStore.GetByIdAsync(request.TelegramUserId, cancellationToken);
        if (customer is null)
        {
            return false;
        }

        var text = request.Status switch
        {
            TelegramServiceRequestStatus.InProgress =>
                $"Ваша заявка #{request.Id} взята в работу.\n\nОшибка: {Title(request)}\nСервисный специалист скоро свяжется с вами.",
            TelegramServiceRequestStatus.Resolved =>
                $"Ваша заявка #{request.Id} закрыта.\n\nСпасибо за обращение.",
            TelegramServiceRequestStatus.Cancelled =>
                $"Ваша заявка #{request.Id} отменена.",
            _ => string.Empty
        };
        return await SendPrivateAsync(customer.TelegramChatId, text, "customer status notification", cancellationToken);
    }

    private async Task<bool> SendContactPrivatelyAsync(
        TelegramServiceRequestSnapshot request,
        TelegramUserSnapshot recipient,
        CancellationToken cancellationToken)
    {
        var contact = await _userStore.GetPrivateContactAsync(request.TelegramUserId, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        var text =
            $"Контакт по заявке #{request.Id}\n" +
            $"Ошибка: {Title(request)}\n" +
            $"Телефон: {contact.PhoneNumber}";
        return await SendPrivateAsync(recipient.TelegramChatId, text, "private contact delivery", cancellationToken);
    }

    private async Task<bool> SendPrivateAsync(
        long chatId,
        string text,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _outboundClient.SendMessageAsync(
                chatId,
                text,
                parseMode: null,
                disableWebPagePreview: true,
                cancellationToken: cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Telegram {Operation} failed; committed service request state remains unchanged.", operation);
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
                "Telegram {Operation} failed; committed service request state remains unchanged. ExceptionType: {ExceptionType}.",
                operation,
                exception.GetType().Name);
            return false;
        }
    }

    private async Task<TelegramServiceQueueCommandResult> BackToCardAsync(
        long requestId,
        CancellationToken cancellationToken)
    {
        var request = await _requestStore.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return NotFound(requestId);
        }

        await RefreshRequestCardAsync(requestId, cancellationToken);
        return Result(await _cardRenderer.RenderAsync(request, cancellationToken));
    }

    private async Task EditAssignmentMenuAsync(
        long requestId,
        TelegramServiceQueueCommandResult menu,
        CancellationToken cancellationToken)
    {
        var request = await _requestStore.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return;
        }

        if (request.NotificationChatId is not null && request.NotificationMessageId is not null)
        {
            var edited = await TryEditAsync(
                request.NotificationChatId.Value,
                request.NotificationMessageId.Value,
                menu.Text,
                menu.ReplyMarkup,
                cancellationToken);
            if (edited)
            {
                return;
            }
        }

        await SendGroupFallbackAsync(menu.Text, menu.ReplyMarkup, request, saveAsNotification: false, cancellationToken);
    }

    private async Task<bool> RefreshRequestCardAsync(
        long requestId,
        CancellationToken cancellationToken)
    {
        var request = await _requestStore.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return false;
        }

        var text = await _cardRenderer.RenderAsync(request, cancellationToken);
        var keyboard = TelegramServiceRequestCardRenderer.Keyboard(request);
        if (request.NotificationChatId is not null && request.NotificationMessageId is not null)
        {
            var edited = await TryEditAsync(
                request.NotificationChatId.Value,
                request.NotificationMessageId.Value,
                text,
                keyboard,
                cancellationToken);
            if (edited)
            {
                var now = DateTimeOffset.UtcNow;
                await _requestStore.UpdateNotificationAsync(
                    new TelegramServiceRequestNotificationUpdate(
                        request.Id,
                        request.NotificationChatId.Value,
                        request.NotificationMessageId.Value,
                        request.NotificationSentAt ?? now,
                        now),
                    cancellationToken);
                return true;
            }
        }

        return await SendGroupFallbackAsync(text, keyboard, request, saveAsNotification: true, cancellationToken);
    }

    private async Task<bool> TryEditAsync(
        long chatId,
        long messageId,
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _outboundClient.EditMessageTextAsync(
                chatId,
                messageId,
                text,
                replyMarkup,
                cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Telegram service request card edit failed; committed state remains unchanged.");
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
                "Telegram service request card edit failed; committed state remains unchanged. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return false;
        }
    }

    private async Task<bool> SendGroupFallbackAsync(
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup,
        TelegramServiceRequestSnapshot request,
        bool saveAsNotification,
        CancellationToken cancellationToken)
    {
        var chatId = _options.ServiceRequests.NotificationChatId;
        if (chatId is null)
        {
            return false;
        }

        try
        {
            var result = await _outboundClient.SendMessageAsync(
                chatId.Value,
                text,
                parseMode: null,
                disableWebPagePreview: true,
                replyMarkup,
                cancellationToken);
            if (result.Succeeded && saveAsNotification && result.MessageId is not null)
            {
                var now = DateTimeOffset.UtcNow;
                await _requestStore.UpdateNotificationAsync(
                    new TelegramServiceRequestNotificationUpdate(
                        request.Id,
                        chatId.Value,
                        result.MessageId.Value,
                        request.NotificationSentAt ?? now,
                        now),
                    cancellationToken);
            }
            else if (!result.Succeeded)
            {
                _logger.LogWarning("Telegram service request card fallback send failed; committed state remains unchanged.");
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
                "Telegram service request card fallback send failed; committed state remains unchanged. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            return false;
        }
    }

    private async Task<string?> AssigneeLabelAsync(long? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            return null;
        }
        var user = await _userStore.GetByIdAsync(id.Value, cancellationToken);
        return user is null ? "специалист" : UserLabel(user);
    }

    private static string UserLabel(TelegramUserSnapshot user) =>
        string.IsNullOrWhiteSpace(user.Username) ? "специалист" : $"@{user.Username.Trim().TrimStart('@')}";

    private static TelegramServiceQueueCommandResult NotFound(long id) =>
        Result($"Заявка #{id} не найдена.");

    private static bool TryParseCallbackData(
        string? data,
        out string action,
        out long requestId,
        out long? targetUserId)
    {
        action = string.Empty;
        requestId = 0;
        targetUserId = null;
        if (string.IsNullOrWhiteSpace(data) ||
            System.Text.Encoding.UTF8.GetByteCount(data) > 64)
        {
            return false;
        }

        var parts = data.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 3 or > 4 ||
            parts[0] != "sr" ||
            !long.TryParse(parts[2], out requestId) ||
            requestId <= 0)
        {
            return false;
        }

        action = parts[1];
        if (action == "as")
        {
            if (parts.Length != 4 || !long.TryParse(parts[3], out var parsedUserId) || parsedUserId <= 0)
            {
                return false;
            }
            targetUserId = parsedUserId;
        }
        else if (parts.Length != 3 || action is not ("t" or "a" or "c" or "d" or "x" or "s" or "b"))
        {
            return false;
        }

        return true;
    }

    private static string Callback(string action, long requestId) => $"sr:{action}:{requestId}";

    private static string CallbackAssign(long requestId, long userId) => $"sr:as:{requestId}:{userId}";

    private static EquipmentDiagnosticTelegramInlineKeyboardButton Button(string text, string callbackData) =>
        new(text, callbackData);

    private static EquipmentDiagnosticTelegramReplyMarkup InlineKeyboard(
        IReadOnlyList<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>> rows) =>
        new(InlineKeyboard: rows);

    private static string UserDisplayName(TelegramUserSnapshot user)
    {
        var fullName = string.Join(
            " ",
            new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }
        return string.IsNullOrWhiteSpace(user.Username)
            ? "Telegram user"
            : $"@{user.Username.Trim().TrimStart('@')}";
    }

    private static TelegramServiceQueueCommandResult Result(
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null) =>
        new(text, replyMarkup);

    private static TelegramServiceQueueCommandResult CallbackResult(string text, string answer) =>
        new(text, CallbackAnswerText: answer, SuppressGroupMessage: true);

    private static string Title(TelegramServiceRequestSnapshot request) =>
        string.IsNullOrWhiteSpace(request.Manufacturer)
            ? request.Code
            : $"{request.Manufacturer} {request.Code}";
}
