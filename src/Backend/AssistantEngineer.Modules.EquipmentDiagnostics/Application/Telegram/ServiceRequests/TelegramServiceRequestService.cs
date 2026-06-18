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
    private readonly ILogger<TelegramServiceRequestService> _logger;

    public TelegramServiceRequestService(
        ITelegramServiceRequestStore requestStore,
        ITelegramDiagnosticCaseStore diagnosticCaseStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        EquipmentDiagnosticTelegramOptions options,
        TelegramDisplayTimeFormatter timeFormatter,
        ILogger<TelegramServiceRequestService>? logger = null)
    {
        _requestStore = requestStore;
        _diagnosticCaseStore = diagnosticCaseStore;
        _outboundClient = outboundClient;
        _options = options;
        _timeFormatter = timeFormatter;
        _logger = logger ?? NullLogger<TelegramServiceRequestService>.Instance;
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
                "Отправьте код ошибки, а после диагностики нажмите «Нужен мастер».";
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

        var userLabel = string.IsNullOrWhiteSpace(update.Username)
            ? "Telegram user"
            : $"@{update.Username.Trim().TrimStart('@')}";
        var phoneSource = request.PhoneNumberSource switch
        {
            TelegramUserPhoneNumberSource.TelegramContact => "Telegram",
            TelegramUserPhoneNumberSource.Manual => "вручную",
            _ => "не указан"
        };
        var text =
            "🛠 Новая сервисная заявка\n\n" +
            $"Заявка: #{request.Id}\n" +
            $"Ошибка: {Title(request)}\n" +
            $"Пользователь: {userLabel}\n" +
            "Телефон: сохранён\n" +
            $"Источник номера: {phoneSource}\n" +
            $"Время: {_timeFormatter.FormatAbsolute(request.CreatedAt)}\n" +
            $"Статус: {StatusLabel(request.Status)}";

        try
        {
            var result = await _outboundClient.SendMessageAsync(
                notificationChatId.Value,
                text,
                parseMode: null,
                disableWebPagePreview: true,
                cancellationToken: cancellationToken);
            if (result.Succeeded)
            {
                _logger.LogInformation("Telegram service request notification sent.");
            }
            else
            {
                _logger.LogWarning("Telegram service request notification failed; request remains created.");
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
        }
    }

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
