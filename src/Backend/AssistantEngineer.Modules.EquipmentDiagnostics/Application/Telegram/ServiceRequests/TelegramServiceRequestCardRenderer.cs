using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;

public sealed class TelegramServiceRequestCardRenderer
{
    private readonly ITelegramUserStore _userStore;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;

    public TelegramServiceRequestCardRenderer(
        ITelegramUserStore userStore,
        TelegramDisplayTimeFormatter timeFormatter)
    {
        _userStore = userStore;
        _timeFormatter = timeFormatter;
    }

    public async Task<string> RenderAsync(
        TelegramServiceRequestSnapshot request,
        CancellationToken cancellationToken = default)
    {
        var assignee = request.AssignedTelegramUserId is null
            ? null
            : await _userStore.GetByIdAsync(request.AssignedTelegramUserId.Value, cancellationToken);
        var assigneeLabel = assignee is null
            ? "не назначен"
            : string.IsNullOrWhiteSpace(assignee.Username)
                ? DisplayName(assignee)
                : $"@{assignee.Username.Trim().TrimStart('@')}";

        var builder = new StringBuilder();
        builder.AppendLine($"🛠 Сервисная заявка #{request.Id}");
        builder.AppendLine();
        builder.AppendLine($"Ошибка: {Title(request)}");
        builder.AppendLine($"Статус: {TelegramServiceRequestService.StatusLabel(request.Status)}");
        builder.AppendLine($"Инженер: {assigneeLabel}");
        builder.AppendLine($"Телефон: {(request.PhoneWasSaved ? "сохранён" : "не сохранён")}");
        builder.Append($"Создана: {_timeFormatter.FormatRelative(request.CreatedAt)}");
        if (request.ClosedAt is not null)
        {
            builder.AppendLine();
            builder.Append($"Закрыта: {_timeFormatter.FormatRelative(request.ClosedAt.Value)}");
        }

        return builder.ToString();
    }

    public static EquipmentDiagnosticTelegramReplyMarkup Keyboard(TelegramServiceRequestSnapshot request) =>
        request.Status switch
        {
            TelegramServiceRequestStatus.New => InlineKeyboard(
            [
                [Button("Взять в работу", Callback("t", request.Id)), Button("Назначить", Callback("a", request.Id))],
                [Button("Контакт", Callback("c", request.Id)), Button("Статус", Callback("s", request.Id))],
                [Button("Отменить", Callback("x", request.Id))]
            ]),
            TelegramServiceRequestStatus.InProgress => InlineKeyboard(
            [
                [Button("Контакт", Callback("c", request.Id)), Button("Закрыть", Callback("d", request.Id))],
                [Button("Назначить", Callback("a", request.Id)), Button("Статус", Callback("s", request.Id))],
                [Button("Отменить", Callback("x", request.Id))]
            ]),
            _ => InlineKeyboard(
            [
                [Button("Статус", Callback("s", request.Id))]
            ])
        };

    private static string DisplayName(TelegramUserSnapshot user)
    {
        var name = string.Join(
            " ",
            new[] { user.FirstName, user.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? "специалист" : name;
    }

    private static string Title(TelegramServiceRequestSnapshot request) =>
        string.IsNullOrWhiteSpace(request.Manufacturer)
            ? request.Code
            : $"{request.Manufacturer} {request.Code}";

    private static string Callback(string action, long requestId) => $"sr:{action}:{requestId}";

    private static EquipmentDiagnosticTelegramInlineKeyboardButton Button(string text, string callbackData) =>
        new(text, callbackData);

    private static EquipmentDiagnosticTelegramReplyMarkup InlineKeyboard(
        IReadOnlyList<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>> rows) =>
        new(InlineKeyboard: rows);
}
