using System.Text;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;

public sealed class TelegramDiagnosticHistoryService
{
    public const int DefaultHistoryLimit = 5;
    private const int SummaryMaxLength = 240;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ITelegramDiagnosticCaseStore _store;
    private readonly TelegramDisplayTimeFormatter _timeFormatter;

    public TelegramDiagnosticHistoryService(
        ITelegramDiagnosticCaseStore store,
        TelegramDisplayTimeFormatter timeFormatter)
    {
        _store = store;
        _timeFormatter = timeFormatter;
    }

    public Task<TelegramDiagnosticCaseSnapshot> RecordCompletedAsync(
        TelegramUserAccessResult access,
        long? conversationSessionId,
        EquipmentDiagnosticBotResponse diagnosis,
        string? manufacturer,
        string code,
        string? equipmentType,
        string? displayContext,
        int? candidateCount,
        CancellationToken cancellationToken = default) =>
        _store.CreateAsync(
            Create(
                access,
                conversationSessionId,
                TelegramDiagnosticCaseStatus.Completed,
                code,
                manufacturer ?? diagnosis.NormalizedManufacturer,
                equipmentType,
                displayContext,
                SafeSummary(diagnosis.AnswerCard?.Summary ?? diagnosis.Message),
                BuildNormalizedRequestJson(
                    diagnosis.NormalizedCode,
                    manufacturer ?? diagnosis.NormalizedManufacturer,
                    equipmentType,
                    displayContext),
                candidateCount),
            cancellationToken);

    public Task<TelegramDiagnosticCaseSnapshot> RecordNotFoundAsync(
        TelegramUserAccessResult access,
        long? conversationSessionId,
        string code,
        string? manufacturer,
        int? candidateCount,
        CancellationToken cancellationToken = default) =>
        _store.CreateAsync(
            Create(
                access,
                conversationSessionId,
                TelegramDiagnosticCaseStatus.NotFound,
                code,
                manufacturer,
                equipmentType: null,
                displayContext: null,
                resultSummary: null,
                normalizedRequestJson: BuildNormalizedRequestJson(code, manufacturer, null, null),
                candidateCount),
            cancellationToken);

    public async Task<string> FormatHistoryAsync(
        TelegramUserSnapshot user,
        CancellationToken cancellationToken = default)
    {
        var cases = await _store.GetLatestForTelegramUserAsync(user.Id, DefaultHistoryLimit, cancellationToken);
        if (cases.Count == 0)
        {
            return EmptyHistoryText();
        }

        var builder = new StringBuilder();
        builder.AppendLine("История диагностик");
        builder.AppendLine();
        for (var index = 0; index < cases.Count; index++)
        {
            var diagnosticCase = cases[index];
            builder.AppendLine($"{index + 1}. {FormatCaseTitle(diagnosticCase)} — {_timeFormatter.FormatRelative(diagnosticCase.CreatedAt)}");
        }

        builder.AppendLine();
        builder.Append("Чтобы проверить новый код, нажмите «Новый код».");
        return builder.ToString();
    }

    public async Task<string> FormatLastAsync(
        TelegramUserSnapshot user,
        CancellationToken cancellationToken = default)
    {
        var diagnosticCase = await _store.GetLastForTelegramUserAsync(user.Id, cancellationToken);
        if (diagnosticCase is null)
        {
            return EmptyHistoryText();
        }

        if (diagnosticCase.Status == TelegramDiagnosticCaseStatus.NotFound)
        {
            return
                "Последний запрос\n\n" +
                $"Код: {FormatCaseTitle(diagnosticCase)}\n" +
                $"Дата: {_timeFormatter.FormatAbsolute(diagnosticCase.CreatedAt)}\n" +
                "Статус: точная расшифровка не найдена.";
        }

        var isConsumer = user.Role == TelegramUserRole.Consumer;
        var summary = isConsumer
            ? ConsumerSafeSummary()
            : TechnicalSummary(diagnosticCase);

        return
            "Последняя диагностика\n\n" +
            $"{FormatCaseTitle(diagnosticCase)}\n" +
            $"Дата: {_timeFormatter.FormatAbsolute(diagnosticCase.CreatedAt)}\n\n" +
            "Возможное значение:\n" +
            $"{summary}\n\n" +
            "Нажмите «Новый код», чтобы проверить другую ошибку.";
    }

    private static TelegramDiagnosticCaseCreate Create(
        TelegramUserAccessResult access,
        long? conversationSessionId,
        TelegramDiagnosticCaseStatus status,
        string code,
        string? manufacturer,
        string? equipmentType,
        string? displayContext,
        string? resultSummary,
        string? normalizedRequestJson,
        int? candidateCount)
    {
        var user = access.User ?? throw new InvalidOperationException("Telegram user is required to record diagnostic history.");
        return new TelegramDiagnosticCaseCreate(
            user.Id,
            conversationSessionId,
            status,
            access.Role,
            access.UsesTechnicalResponse ? TelegramDiagnosticCaseResponseMode.Technical : TelegramDiagnosticCaseResponseMode.Consumer,
            Required(SafeText(code, 64), "Diagnostic code is required."),
            SafeText(manufacturer, 128),
            SafeText(equipmentType, 128),
            SafeText(displayContext, 128),
            SafeText(resultSummary, SummaryMaxLength),
            normalizedRequestJson,
            candidateCount,
            user.HasPhoneNumber,
            user.PhoneNumberSource,
            DateTimeOffset.UtcNow);
    }

    private static string EmptyHistoryText() =>
        "История пока пустая. Отправьте код ошибки, например: Gree H5.";

    private static string FormatCaseTitle(TelegramDiagnosticCaseSnapshot diagnosticCase) =>
        string.IsNullOrWhiteSpace(diagnosticCase.Manufacturer)
            ? diagnosticCase.Code
            : $"{diagnosticCase.Manufacturer} {diagnosticCase.Code}";

    private static string ConsumerSafeSummary() =>
        "Сработала защита оборудования. Точное значение зависит от модели и места отображения ошибки.";

    private static string TechnicalSummary(TelegramDiagnosticCaseSnapshot diagnosticCase) =>
        string.IsNullOrWhiteSpace(diagnosticCase.ResultSummary)
            ? ConsumerSafeSummary()
            : diagnosticCase.ResultSummary;

    private static string? BuildNormalizedRequestJson(
        string? code,
        string? manufacturer,
        string? equipmentType,
        string? displayContext)
    {
        var payload = new
        {
            code = SafeText(code, 64),
            manufacturer = SafeText(manufacturer, 128),
            equipmentType = SafeText(equipmentType, 128),
            displayContext = SafeText(displayContext, 128)
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string? SafeSummary(string? value) =>
        SafeText(value, SummaryMaxLength);

    private static string Required(string? value, string message) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(message) : value;

    private static string? SafeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = new string(value.Trim()
            .Where(character => !char.IsControl(character) || character is '\r' or '\n' or '\t')
            .ToArray());
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return string.Concat(trimmed.AsSpan(0, maxLength - 3).TrimEnd(), "...");
    }
}
