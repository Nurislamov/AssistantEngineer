using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;

public sealed class TelegramManualLibraryService
{
    public const string ManualLibraryButton = "📘 Руководства";
    private const string ManualRequestCommand = "/manuals";
    private const string ManualRegisterCommand = "/manual_register";

    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly ITelegramDiagnosticCaseStore _historyStore;
    private readonly IErrorKnowledgeLocalizationSource _localizedKnowledge;
    private readonly ITelegramManualRegistrySource _manualRegistry;
    private readonly ITelegramManualFileBindingStore _bindingStore;

    public TelegramManualLibraryService(
        EquipmentDiagnosticTelegramOptions options,
        ITelegramDiagnosticCaseStore historyStore,
        IErrorKnowledgeLocalizationSource localizedKnowledge,
        ITelegramManualRegistrySource manualRegistry,
        ITelegramManualFileBindingStore bindingStore)
    {
        _options = options;
        _historyStore = historyStore;
        _localizedKnowledge = localizedKnowledge;
        _manualRegistry = manualRegistry;
        _bindingStore = bindingStore;
    }

    public static bool IsManualRequest(string? text) =>
        string.Equals(text?.Trim(), ManualRequestCommand, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(text?.Trim(), ManualLibraryButton, StringComparison.Ordinal);

    public static bool IsManualRegistration(string? text) =>
        text?.TrimStart().StartsWith(ManualRegisterCommand, StringComparison.OrdinalIgnoreCase) == true;

    public async Task<TelegramManualLibraryResult> RequestManualsAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return Text("Библиотека руководств сейчас выключена.");
        }

        if (!CanRequestManuals(access.Role))
        {
            return Text("Руководства доступны только техническим ролям: монтажник, сервис-инженер, админ или владелец.");
        }

        if (access.User is null)
        {
            return Text("Сначала отправьте код ошибки, например: Gree H5.");
        }

        var last = await _historyStore.GetLastForTelegramUserAsync(access.User.Id, cancellationToken);
        if (last is null || last.Status != TelegramDiagnosticCaseStatus.Completed)
        {
            return Text("Сначала выполните диагностику кода, затем запросите руководства.");
        }

        var manuals = ResolveManuals(last, access.Role);
        if (manuals.Count == 0)
        {
            return Text("Для последнего кода пока нет подключенного руководства в библиотеке.");
        }

        var maxFiles = Math.Clamp(_options.ManualLibrary.MaxFilesPerRequest, 1, 10);
        var selectedManuals = manuals.Take(maxFiles).ToArray();
        var missing = new List<TelegramManualRegistryEntry>();
        var messages = new List<EquipmentDiagnosticTelegramOutboundMessage>();
        foreach (var manual in selectedManuals)
        {
            var binding = await _bindingStore.GetAsync(manual.ManualId, cancellationToken);
            if (binding is null)
            {
                missing.Add(manual);
                continue;
            }

            messages.Add(new EquipmentDiagnosticTelegramOutboundMessage(
                $"Руководство: {DisplayName(manual)}",
                ReplyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access),
                DocumentFileId: binding.TelegramFileId,
                DocumentFileName: binding.OriginalFileName));
        }

        var text = BuildRequestText(selectedManuals, messages.Count, missing, manuals.Count > selectedManuals.Length);
        IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? outbound = messages.Count == 0
            ? null
            :
            [
                new EquipmentDiagnosticTelegramOutboundMessage(
                    text,
                    ReplyMarkup: TelegramDiagnosticConversationService.MainKeyboard(access)),
                .. messages
            ];

        return new TelegramManualLibraryResult(text, [], outbound);
    }

    public async Task<TelegramManualLibraryResult> RegisterManualAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return Text("Библиотека руководств сейчас выключена.");
        }

        if (!CanRegisterManuals(access.Role))
        {
            return Text("Регистрация файлов руководств доступна только админу или владельцу.");
        }

        var parts = update.Text?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        if (parts.Length < 2)
        {
            return Text("Укажите manualId из реестра: /manual_register <manualId>. Файл должен быть приложен документом Telegram.");
        }

        if (parts.Length > 2)
        {
            return Text("Идентификатор файла не принимается текстом. Отправьте документ с подписью /manual_register <manualId> или ответьте командой на документ.");
        }

        var manual = _manualRegistry.GetManuals()
            .FirstOrDefault(item => item.ManualId.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
        if (manual is null)
        {
            return Text("Руководство с таким идентификатором не найдено в реестре.");
        }

        if (!IsAllowedForRole(manual, access.Role))
        {
            return Text("Это руководство есть в реестре, но пока не разрешено для Telegram-библиотеки.");
        }

        var document = GetRegistrationDocument(update);
        if (document is null)
        {
            return Text("Файл не найден. Отправьте документ с подписью /manual_register <manualId> или ответьте командой на сообщение с документом.");
        }

        if (!IsAllowedDocument(manual, document.Value.FileName, document.Value.MimeType))
        {
            return Text("Тип файла не подходит для этого руководства. Разрешены только проверенные форматы из реестра.");
        }

        await _bindingStore.UpsertAsync(
            new TelegramManualFileBinding(
                manual.ManualId,
                document.Value.FileId,
                SafeFileName(document.Value.FileName),
                SafeContentType(document.Value.MimeType),
                DateTimeOffset.UtcNow,
                "TelegramStorage",
                access.Role.ToString()),
            cancellationToken);

        return Text($"Файл руководства подключен: {DisplayName(manual)}.");
    }

    private IReadOnlyList<TelegramManualRegistryEntry> ResolveManuals(
        TelegramDiagnosticCaseSnapshot diagnosticCase,
        TelegramUserRole role)
    {
        var manuals = _manualRegistry.GetManuals();
        var entries = _localizedKnowledge.GetEntries()
            .Where(entry =>
                string.Equals(entry.Code, diagnosticCase.Code, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(diagnosticCase.Manufacturer) ||
                 string.Equals(entry.Manufacturer, diagnosticCase.Manufacturer, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (!string.IsNullOrWhiteSpace(diagnosticCase.EquipmentType))
        {
            var narrowed = entries
                .Where(entry => string.Equals(EntryEquipmentLabel(entry), diagnosticCase.EquipmentType, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (narrowed.Length > 0)
            {
                entries = narrowed;
            }
        }

        return entries
            .SelectMany(entry => ManualIds(entry, manuals))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(manualId => manuals.FirstOrDefault(manual => manual.ManualId.Equals(manualId, StringComparison.OrdinalIgnoreCase)))
            .Where(manual => manual is not null && IsAllowedForRole(manual, role))
            .Select(manual => manual!)
            .OrderBy(manual => DisplayName(manual), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> ManualIds(
        ErrorKnowledgeEntryV2 entry,
        IReadOnlyList<TelegramManualRegistryEntry> manuals)
    {
        foreach (var reference in entry.SourceReferences)
        {
            if (!string.IsNullOrWhiteSpace(reference.ManualId))
            {
                yield return reference.ManualId;
            }
        }

        if (entry.SourceReferences.Count > 0)
        {
            yield break;
        }

        var sourceName = entry.SourceName.Trim();
        var exact = manuals.FirstOrDefault(manual =>
            string.Equals(manual.DocumentTitle, sourceName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Path.GetFileNameWithoutExtension(manual.FileName), sourceName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            yield return exact.ManualId;
        }
    }

    private static string BuildRequestText(
        IReadOnlyList<TelegramManualRegistryEntry> manuals,
        int boundCount,
        IReadOnlyList<TelegramManualRegistryEntry> missing,
        bool truncated)
    {
        var builder = new StringBuilder();
        builder.AppendLine(boundCount > 0
            ? "Отправляю доступные руководства по последней диагностике."
            : "Руководства по последней диагностике известны, но файлы пока не подключены.");

        if (missing.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Файл пока не подключен:");
            foreach (var manual in missing)
            {
                builder.AppendLine($"- {DisplayName(manual)}");
            }
        }

        if (truncated)
        {
            builder.AppendLine();
            builder.AppendLine("Часть руководств не отправлена из-за лимита на один запрос.");
        }

        return builder.ToString().Trim();
    }

    private static bool CanRequestManuals(TelegramUserRole role) =>
        role is TelegramUserRole.Installer or TelegramUserRole.Engineer or TelegramUserRole.Admin or TelegramUserRole.Owner;

    private static bool CanRegisterManuals(TelegramUserRole role) =>
        role is TelegramUserRole.Admin or TelegramUserRole.Owner;

    private static bool IsAllowedForRole(TelegramManualRegistryEntry manual, TelegramUserRole role) =>
        manual.EligibleForTelegramLibrary &&
        manual.AllowedRoles.Contains(role) &&
        !manual.DeniedRoles.Contains(role);

    private static string EntryEquipmentLabel(ErrorKnowledgeEntryV2 entry) =>
        entry.SignalType is ErrorKnowledgeSignalType.Debug or ErrorKnowledgeSignalType.Commissioning ||
        entry.PackageId.Contains("debugging", StringComparison.OrdinalIgnoreCase)
            ? RussianDiagnosticTerminology.SignalTypeLabel(ErrorKnowledgeSignalType.Commissioning)
            : entry.SignalType is ErrorKnowledgeSignalType.Status or ErrorKnowledgeSignalType.Maintenance
                ? RussianDiagnosticTerminology.SignalTypeLabel(ErrorKnowledgeSignalType.Status)
                : RussianDiagnosticTerminology.EquipmentTypeLabel(entry.EquipmentType);

    private bool IsAllowedDocument(
        TelegramManualRegistryEntry manual,
        string? fileName,
        string? mimeType)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(extension) ||
            !_options.ManualLibrary.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var expected = manual.FileFormat.Trim();
        if (string.IsNullOrWhiteSpace(expected) || expected.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return extension.TrimStart('.').Equals(expected, StringComparison.OrdinalIgnoreCase) ||
            expected.Equals("PDF", StringComparison.OrdinalIgnoreCase) && extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private static RegistrationDocument? GetRegistrationDocument(EquipmentDiagnosticTelegramUpdate update)
    {
        if (!string.IsNullOrWhiteSpace(update.DocumentFileId))
        {
            return new RegistrationDocument(
                update.DocumentFileId,
                update.DocumentFileName,
                update.DocumentMimeType,
                update.DocumentFileSize);
        }

        return string.IsNullOrWhiteSpace(update.ReplyToDocumentFileId)
            ? null
            : new RegistrationDocument(
                update.ReplyToDocumentFileId,
                update.ReplyToDocumentFileName,
                update.ReplyToDocumentMimeType,
                update.ReplyToDocumentFileSize);
    }

    private static string DisplayName(TelegramManualRegistryEntry manual) =>
        string.IsNullOrWhiteSpace(manual.DocumentCode)
            ? manual.DocumentTitle
            : $"{manual.DocumentTitle} ({manual.DocumentCode})";

    private static string? SafeFileName(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Path.GetFileName(value.Trim());

    private static string? SafeContentType(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().Length > 128
                ? value.Trim()[..128]
                : value.Trim();

    private static TelegramManualLibraryResult Text(string text) => new(text, []);

    private readonly record struct RegistrationDocument(
        string FileId,
        string? FileName,
        string? MimeType,
        long? FileSize);
}
