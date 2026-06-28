using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public sealed class TelegramServiceRequestService
{
    public const int DefaultRequestLimit = 5;

    private readonly ITelegramServiceRequestStore _requestStore;
    private readonly ITelegramDiagnosticCaseStore _diagnosticCaseStore;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;
    private readonly TelegramServiceRequestCardRenderer _cardRenderer;
    private readonly ILogger<TelegramServiceRequestService> _logger;
    private readonly TelegramServiceRequestEventService? _eventService;

    public TelegramServiceRequestService(
        ITelegramServiceRequestStore requestStore,
        ITelegramDiagnosticCaseStore diagnosticCaseStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        EquipmentDiagnosticTelegramOptions options,
        TelegramDisplayTimeFormatter timeFormatter,
        TelegramServiceRequestCardRenderer cardRenderer,
        ILogger<TelegramServiceRequestService>? logger = null,
        TelegramServiceRequestEventService? eventService = null)
    {
        _requestStore = requestStore;
        _diagnosticCaseStore = diagnosticCaseStore;
        _outboundClient = outboundClient;
        _options = options;
        _timeFormatter = timeFormatter;
        _cardRenderer = cardRenderer;
        _logger = logger ?? NullLogger<TelegramServiceRequestService>.Instance;
        _eventService = eventService;
    }

    public async Task<TelegramServiceRequestAttemptResult> CreateFromLatestAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        var user = access.User;
        if (user is null)
        {
            return NoDiagnosticCase();
        }

        var diagnosticCase = await _diagnosticCaseStore.GetLastForTelegramUserAsync(user.Id, cancellationToken);
        if (diagnosticCase is null)
        {
            return NoDiagnosticCase();
        }

        if (!user.HasPhoneNumber)
        {
            return new TelegramServiceRequestAttemptResult(
                TelegramServiceRequestAttemptStatus.PhoneMissing,
                null,
                "Чтобы мастер мог связаться с вами, сначала укажите номер телефона.");
        }

        var createResult = await _requestStore.CreateIfNoActiveAsync(
            new TelegramServiceRequestCreate(
                user.Id,
                diagnosticCase.Id,
                diagnosticCase.Code,
                diagnosticCase.Manufacturer,
                diagnosticCase.EquipmentType,
                diagnosticCase.DisplayContext,
                PhoneWasSaved: true,
                user.PhoneNumberSource,
                access.Role,
                update.ReceivedAt ?? DateTimeOffset.UtcNow),
            cancellationToken);

        if (!createResult.Created)
        {
            return new TelegramServiceRequestAttemptResult(
                TelegramServiceRequestAttemptStatus.Existing,
                createResult.Request,
                "По этой диагностике уже есть заявка.\n\n" +
                $"Статус: {StatusLabel(createResult.Request.Status)}\n" +
                $"Создана: {_timeFormatter.FormatRelative(createResult.Request.CreatedAt)}");
        }

        _logger.LogInformation(
            "Telegram service request created. Status: {Status}. NotificationConfigured: {NotificationConfigured}.",
            createResult.Request.Status,
            _options.ServiceRequests.NotificationChatId is not null);

        await RecordEventAsync(
            new TelegramServiceRequestEventCreate(
                createResult.Request.Id,
                TelegramServiceRequestEventType.Created,
                user.Id,
                null,
                null,
                TelegramServiceRequestStatus.New,
                true,
                "Service request created.",
                null,
                createResult.Request.CreatedAt),
            cancellationToken);

        await NotifyGroupAsync(update, createResult.Request, cancellationToken);

        return new TelegramServiceRequestAttemptResult(
            TelegramServiceRequestAttemptStatus.Created,
            createResult.Request,
            "Заявка создана\n\n" +
            $"Ошибка: {Title(createResult.Request)}\n" +
            $"Статус: {StatusLabel(createResult.Request.Status)}\n" +
            "Телефон для связи: сохранён\n\n" +
            "Сервисный специалист свяжется с вами.");
    }

    public async Task<string> FormatRequestsAsync(
        TelegramUserSnapshot user,
        CancellationToken cancellationToken = default)
    {
        var requests = await _requestStore.GetLatestForTelegramUserAsync(user.Id, DefaultRequestLimit, cancellationToken);
        if (requests.Count == 0)
        {
            return "У вас пока нет сервисных заявок.\n\n" +
                "Отправьте код ошибки, а после диагностики нажмите «Оставить заявку».";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Мои заявки");
        builder.AppendLine();
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            builder.AppendLine(
                $"{index + 1}. {Title(request)} — {StatusLabel(request.Status)} — {_timeFormatter.FormatRelative(request.CreatedAt)}");
        }

        builder.AppendLine();
        builder.Append("Сервисный специалист свяжется с вами по сохранённому номеру.");
        return builder.ToString();
    }

    public static string StatusLabel(TelegramServiceRequestStatus status) =>
        status switch
        {
            TelegramServiceRequestStatus.New => "новая",
            TelegramServiceRequestStatus.InProgress => "в работе",
            TelegramServiceRequestStatus.Resolved => "закрыта",
            TelegramServiceRequestStatus.Cancelled => "отменена",
            _ => "неизвестно"
        };

    private async Task NotifyGroupAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramServiceRequestSnapshot request,
        CancellationToken cancellationToken)
    {
        var notificationChatId = _options.ServiceRequests.NotificationChatId;
        if (notificationChatId is null || !_options.ServiceRequests.NotifyOnCreate)
        {
            if (notificationChatId is null)
            {
                _logger.LogWarning("Telegram service request notification chat is not configured.");
            }

            return;
        }

        var text = await _cardRenderer.RenderAsync(request, cancellationToken);

        try
        {
            var result = await _outboundClient.SendMessageAsync(
                notificationChatId.Value,
                text,
                parseMode: null,
                disableWebPagePreview: true,
                replyMarkup: TelegramServiceRequestCardRenderer.Keyboard(request),
                cancellationToken: cancellationToken);
            if (result.Succeeded)
            {
                if (result.MessageId is not null)
                {
                    var now = DateTimeOffset.UtcNow;
                    await _requestStore.UpdateNotificationAsync(
                        new TelegramServiceRequestNotificationUpdate(
                            request.Id,
                            notificationChatId.Value,
                            result.MessageId.Value,
                            now,
                            now),
                        cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Telegram service request notification returned without a message id.");
                }
                _logger.LogInformation("Telegram service request notification sent.");
                await RecordEventAsync(
                    DeliveryEvent(request.Id, TelegramServiceRequestEventType.NotificationSent, true),
                    cancellationToken);
            }
            else
            {
                _logger.LogWarning("Telegram service request notification failed; request remains created.");
                await RecordEventAsync(
                    DeliveryEvent(request.Id, TelegramServiceRequestEventType.NotificationFailed, false),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Telegram service request notification failed; request remains created. ExceptionType: {ExceptionType}.",
                exception.GetType().Name);
            await RecordEventAsync(
                DeliveryEvent(request.Id, TelegramServiceRequestEventType.NotificationFailed, false),
                cancellationToken);
        }
    }

    private Task RecordEventAsync(
        TelegramServiceRequestEventCreate request,
        CancellationToken cancellationToken) =>
        _eventService?.AppendSafeAsync(request, cancellationToken) ?? Task.CompletedTask;

    private static TelegramServiceRequestEventCreate DeliveryEvent(
        long requestId,
        TelegramServiceRequestEventType type,
        bool succeeded) =>
        new(
            requestId,
            type,
            null,
            null,
            null,
            null,
            succeeded,
            succeeded ? "Service group notification sent." : "Service group notification failed.",
            null,
            DateTimeOffset.UtcNow);

    private static TelegramServiceRequestAttemptResult NoDiagnosticCase() =>
        new(
            TelegramServiceRequestAttemptStatus.NoDiagnosticCase,
            null,
            "Сначала отправьте код ошибки, например: Gree H5.");

    private static string Title(TelegramServiceRequestSnapshot request) =>
        string.IsNullOrWhiteSpace(request.Manufacturer)
            ? request.Code
            : $"{request.Manufacturer} {request.Code}";
}
