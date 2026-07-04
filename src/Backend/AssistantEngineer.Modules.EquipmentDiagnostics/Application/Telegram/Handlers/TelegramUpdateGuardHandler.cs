using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

internal sealed class TelegramUpdateGuardHandler(
    EquipmentDiagnosticTelegramOptions options,
    ITelegramUserStore userStore) : ITelegramUpdateHandler
{
    public async Task<EquipmentDiagnosticTelegramResponse?> TryHandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken)
    {
        if (!options.IsEnabled)
        {
            return Response(update.ChatId, string.Empty, EquipmentDiagnosticTelegramResponseKind.Ignored);
        }

        TelegramUserSnapshot? user = null;
        if (IsPrivateChat(update.ChatType))
        {
            user = await userStore.GetByChatIdAsync(update.ChatId, cancellationToken);
        }

        if (user is null && update.UserId is not null)
        {
            user = await userStore.GetByTelegramUserIdAsync(update.UserId.Value, cancellationToken);
        }

        if (user?.IsBlocked != true)
        {
            return null;
        }

        if (IsPrivateChat(update.ChatType))
        {
            await userStore.MarkAccessDeniedAsync(update.ChatId, cancellationToken);
        }

        return Response(
            update.ChatId,
            EquipmentDiagnosticTelegramAdapter.BlockedUserMessage,
            EquipmentDiagnosticTelegramResponseKind.Reply);
    }

    private static bool IsPrivateChat(string? chatType) =>
        string.IsNullOrWhiteSpace(chatType) ||
        chatType.Equals("private", StringComparison.OrdinalIgnoreCase);

    private static EquipmentDiagnosticTelegramResponse Response(
        long chatId,
        string text,
        EquipmentDiagnosticTelegramResponseKind responseKind) =>
        new(
            chatId,
            text,
            responseKind,
            ParseMode: null,
            DisableWebPagePreview: true,
            Warnings: [],
            InternalDecisionTrace: null);
}
