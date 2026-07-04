using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

internal sealed class TelegramCallbackUpdateHandler(
    ITelegramUserAccessService accessService,
    TelegramServiceRequestQueueService? serviceRequestQueueService = null,
    TelegramServiceRequestDialogService? serviceRequestDialogService = null,
    TelegramAdminUserManagementService? adminUserManagementService = null,
    TelegramUserOverviewService? userOverviewService = null,
    TelegramBroadcastService? broadcastService = null,
    TelegramManualLibraryService? manualLibraryService = null) : ITelegramUpdateHandler
{
    public async Task<EquipmentDiagnosticTelegramResponse?> TryHandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(update.CallbackQueryId))
        {
            return null;
        }

        if (TelegramManualLibraryService.IsDiagnosticManualCallback(update.CallbackData))
        {
            var callbackAccess = await accessService.ResolveAccessAsync(update, cancellationToken);
            if (!callbackAccess.IsAllowed)
            {
                return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
            }

            var manualResult = manualLibraryService is null
                ? new TelegramManualLibraryResult(
                    $"{TelegramHtml.Bold("Мануал недоступен")}\n\nСервис мануалов сейчас недоступен.",
                    [],
                    ParseMode: TelegramHtml.ParseMode,
                    CallbackAnswerText: "Мануал недоступен")
                : await manualLibraryService.RequestDiagnosticGuideAsync(update, callbackAccess, cancellationToken);
            return Response(
                update.ChatId,
                manualResult.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                manualResult.Warnings,
                manualResult.ReplyMarkup ?? TelegramDiagnosticConversationService.MainKeyboard(callbackAccess),
                manualResult.Messages,
                callbackAnswerText: manualResult.CallbackAnswerText,
                parseMode: manualResult.ParseMode);
        }

        if (TelegramManualLibraryService.IsLibraryCallback(update.CallbackData))
        {
            var callbackAccess = await accessService.ResolveAccessAsync(update, cancellationToken);
            if (!callbackAccess.IsAllowed)
            {
                return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
            }

            var manualResult = manualLibraryService is null
                ? new TelegramManualLibraryResult("Действие библиотеки недоступно или устарело.", [], CallbackAnswerText: "Недоступно")
                : await manualLibraryService.HandleLibraryCallbackAsync(update, callbackAccess, cancellationToken);
            return Response(
                update.ChatId,
                manualResult.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                manualResult.Warnings,
                manualResult.ReplyMarkup ?? TelegramDiagnosticConversationService.MainKeyboard(callbackAccess),
                manualResult.Messages,
                callbackAnswerText: manualResult.CallbackAnswerText,
                parseMode: manualResult.ParseMode,
                editMessageId: update.MessageId);
        }

        if (TelegramManualLibraryService.IsManualBindCallback(update.CallbackData))
        {
            var callbackAccess = await accessService.ResolveAccessAsync(update, cancellationToken);
            if (!callbackAccess.IsAllowed)
            {
                return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
            }

            var manualResult = manualLibraryService is null
                ? new TelegramManualLibraryResult("Действие недоступно или устарело.", [], CallbackAnswerText: "Недоступно")
                : await manualLibraryService.HandleManualBindCallbackAsync(update, callbackAccess, cancellationToken);
            return Response(
                update.ChatId,
                manualResult.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                manualResult.Warnings,
                manualResult.ReplyMarkup ?? TelegramDiagnosticConversationService.MainKeyboard(callbackAccess),
                manualResult.Messages,
                callbackAnswerText: manualResult.CallbackAnswerText,
                parseMode: manualResult.ParseMode);
        }

        if (update.CallbackData?.StartsWith("au:", StringComparison.Ordinal) == true)
        {
            var callbackAccess = await accessService.ResolveAccessAsync(update, cancellationToken);
            if (callbackAccess.User is not
                { Role: TelegramUserRole.Owner, IsEnabled: true, IsBlocked: false })
            {
                return Response(
                    update.ChatId,
                    "Раздел пользователей доступен только владельцу.",
                    EquipmentDiagnosticTelegramResponseKind.Reply,
                    callbackAnswerText: "Нет доступа");
            }

            var adminResult = adminUserManagementService is null
                ? new TelegramAdminUserManagementResult(
                    "Действие недоступно или устарело.",
                    CallbackAnswerText: "Ошибка действия",
                    SuppressOutbound: true)
                : await adminUserManagementService.HandleCallbackAsync(update, cancellationToken);
            return Response(
                update.ChatId,
                adminResult.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: adminResult.ReplyMarkup,
                callbackAnswerText: adminResult.CallbackAnswerText,
                suppressOutbound: adminResult.SuppressOutbound);
        }

        if (TelegramUserOverviewService.IsCallback(update.CallbackData))
        {
            var callbackAccess = await accessService.ResolveAccessAsync(update, cancellationToken);
            if (!callbackAccess.IsAllowed)
            {
                return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
            }

            var userOverviewResult = userOverviewService is null
                ? new TelegramUserOverviewResult("Раздел пользователей сейчас недоступен.", CallbackAnswerText: "Недоступно")
                : await userOverviewService.HandleCallbackAsync(update, callbackAccess, cancellationToken);
            return Response(
                update.ChatId,
                userOverviewResult.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: userOverviewResult.ReplyMarkup,
                callbackAnswerText: userOverviewResult.CallbackAnswerText,
                editMessageId: update.MessageId);
        }

        if (TelegramBroadcastService.IsCallback(update.CallbackData))
        {
            var callbackAccess = await accessService.ResolveAccessAsync(update, cancellationToken);
            if (!callbackAccess.IsAllowed)
            {
                return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
            }

            var broadcastResult = broadcastService is null
                ? new TelegramBroadcastResult("Рассылка сейчас недоступна.", CallbackAnswerText: "Недоступно")
                : await broadcastService.HandleCallbackAsync(update, callbackAccess, cancellationToken);
            return Response(
                update.ChatId,
                broadcastResult.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: broadcastResult.ReplyMarkup,
                callbackAnswerText: broadcastResult.CallbackAnswerText,
                editMessageId: update.MessageId);
        }

        if (TelegramServiceRequestDialogService.IsCallback(update.CallbackData))
        {
            var dialogResult = serviceRequestDialogService is null
                ? new TelegramServiceRequestDialogResult(
                    "Диалог по заявке сейчас недоступен.",
                    CallbackAnswerText: "Недоступно",
                    SuppressOutbound: true)
                : await serviceRequestDialogService.HandleCallbackAsync(update, cancellationToken);
            return Response(
                update.ChatId,
                dialogResult.Text,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                replyMarkup: dialogResult.ReplyMarkup,
                callbackAnswerText: dialogResult.CallbackAnswerText,
                suppressOutbound: dialogResult.SuppressOutbound);
        }

        var result = serviceRequestQueueService is null
            ? new TelegramServiceQueueCommandResult("Действие недоступно или устарело.")
            : await serviceRequestQueueService.HandleCallbackAsync(update, cancellationToken);
        return Response(
            update.ChatId,
            result.Text,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            replyMarkup: result.ReplyMarkup,
            callbackAnswerText: result.CallbackAnswerText,
            suppressOutbound: result.SuppressGroupMessage);
    }

    private static EquipmentDiagnosticTelegramResponse Response(
        long chatId,
        string text,
        EquipmentDiagnosticTelegramResponseKind responseKind,
        IReadOnlyList<string>? warnings = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? messages = null,
        string? callbackAnswerText = null,
        bool suppressOutbound = false,
        string? parseMode = null,
        long? editMessageId = null) =>
        new(
            chatId,
            text,
            responseKind,
            ParseMode: parseMode,
            DisableWebPagePreview: true,
            warnings ?? [],
            InternalDecisionTrace: null,
            Messages: ApplyEditMessageId(
                messages ??
                (replyMarkup is null
                    ? null
                    :
                    [
                        new EquipmentDiagnosticTelegramOutboundMessage(
                            text,
                            ParseMode: parseMode,
                            DisableWebPagePreview: true,
                            replyMarkup)
                    ]),
                editMessageId),
            CallbackAnswerText: callbackAnswerText,
            SuppressOutbound: suppressOutbound);

    private static IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? ApplyEditMessageId(
        IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? messages,
        long? editMessageId)
    {
        if (messages is null || editMessageId is null)
        {
            return messages;
        }

        var result = new List<EquipmentDiagnosticTelegramOutboundMessage>(messages.Count);
        var applied = false;
        foreach (var message in messages)
        {
            if (!applied && string.IsNullOrWhiteSpace(message.DocumentFileId))
            {
                result.Add(message with { EditMessageId = editMessageId });
                applied = true;
                continue;
            }

            result.Add(message);
        }

        return result;
    }
}
