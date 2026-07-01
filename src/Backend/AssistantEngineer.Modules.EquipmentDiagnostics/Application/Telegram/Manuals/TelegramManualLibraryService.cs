using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Broadcasts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;

public sealed class TelegramManualLibraryService
{
    public const string ManualLibraryButton = "📘 Руководства";
    public const string DiagnosticManualButton = "📄 Мануал";
    public const string DiagnosticManualCallbackData = "dm:open";
    private const string DiagnosticManualFilePrefix = "dm:file:";
    private const int TelegramCallbackDataMaxBytes = 64;
    private const int TelegramInlineButtonTextMaxBytes = 64;
    public const string LibraryButton = "📚 Библиотека";
    public const string DiagnosticGuideButton = "📘 Руководство";
    private const string LibraryCommand = "/library";
    private const string ManualRequestCommand = "/manuals";
    private const string ManualRegisterCommand = "/manual_register";
    private const string ManualUnregisterCommand = "/manual_unregister";
    private const string ManualBindingsCommand = "/manual_bindings";
    private const string ManualBindCommand = "/manual_bind";
    private const string ManualBindCallbackPrefix = "mb:";
    private const string ManualBindStart = "mb:start";
    private const string ManualBindBrandPrefix = "mb:b:";
    private const string ManualBindSectionPrefix = "mb:sec:";
    private const string ManualBindSeriesPrefix = "mb:s:";
    private const string ManualBindDocumentTypePrefix = "mb:dt:";
    private const string ManualBindConfirm = "mb:c:bind";
    private const string ManualBindReplace = "mb:c:replace";
    private const string ManualBindCancel = "mb:c:cancel";
    private const string LibraryCallbackPrefix = "lib:";
    private const string LibraryGrantPrefix = "lib:g:";
    private const string LibraryRevokePrefix = "lib:r:";
    private const string LibraryApprovePrefix = "lib:approve:";
    private const string LibraryRejectPrefix = "lib:reject:";
    private const string LibraryRequestAccessCallback = "lib:req";
    private const string LibraryOpenCallback = "lib:open";
    private const string LibraryGreeCallback = "lib:brand:gree";
    private const string LibraryRemotesCallback = "lib:brand:remotes";
    private const string LibraryGreeOutdoorCallback = "lib:gree:outdoor";
    private const string LibraryGreeProductPrefix = "lib:gree:outdoor:";
    private const string LibraryGreeSectionPrefix = "lib:gree:section:";
    private const string LibraryFilePrefix = "lib:file:";
    private const string LibraryFileByIdPrefix = "lib:f:";
    private const string LibraryIndoorCategoryPrefix = "lib:i:";
    private const string LibraryControllerCategoryPrefix = "lib:c:";
    private const string LibraryRequestsCallback = "lib:reqs";
    private const string LibraryAccessCallback = "lib:access";
    private const string LibraryCancelCallback = "lib:cancel";
    private const string BrandGree = "Gree";
    private const string OutdoorSectionSlug = "outdoor";
    private const string IndoorSectionSlug = "indoor";
    private const string ControllersSectionSlug = "controllers";
    private const string AccessoriesSectionSlug = "accessories";
    private const string UMatchSectionSlug = "umatch";
    private const string ErvSectionSlug = "erv";
    private const int FreeSectionPageSize = 8;

    private static readonly IReadOnlyList<ManualSeriesOption> SupportedSeries =
    [
        new("gmv6", "GMV6", "GMV6", "Gree GMV6 Service Manual EN.pdf"),
        new("gmv6-hr", "GMV6 HR", "GMV6 HR", "Gree GMV6 HR Service Manual EN.pdf"),
        new("gmv-mini", "GMV Mini / Slim", "GMV Mini", "Gree GMV Mini Slim Service Manual EN.pdf"),
        new("gmv-x", "GMV X", "GMV X", "Gree GMV X Service Manual EN.pdf"),
        new("gmv9-flex", "GMV9 Flex", "GMV9 Flex", "Gree GMV9 Flex Service Manual EN Rev B.pdf")
    ];

    private static readonly IReadOnlySet<TelegramLibraryDocumentType> VisibleLibraryDocumentTypes =
        new HashSet<TelegramLibraryDocumentType>
        {
            TelegramLibraryDocumentType.ServiceManual,
            TelegramLibraryDocumentType.OwnerManual,
            TelegramLibraryDocumentType.ControllerGuide
        };

    private static readonly IReadOnlyList<ManualLibrarySectionOption> LibrarySections =
    [
        new(OutdoorSectionSlug, "Наружные", "Outdoor", true,
            [TelegramLibraryDocumentType.ServiceManual, TelegramLibraryDocumentType.OwnerManual]),
        new(IndoorSectionSlug, "Внутренние", "Indoor", false,
            [TelegramLibraryDocumentType.OwnerManual, TelegramLibraryDocumentType.ServiceManual]),
        new(ControllersSectionSlug, "Пульты / Controllers", "Controllers", false,
            [TelegramLibraryDocumentType.ControllerGuide]),
        new(UMatchSectionSlug, "Полупром / U-Match", "U-Match R32", false,
            [TelegramLibraryDocumentType.ServiceManual, TelegramLibraryDocumentType.OwnerManual]),
        new(ErvSectionSlug, "Вентиляция ERV", "ERV B Series", false,
            [TelegramLibraryDocumentType.ServiceManual, TelegramLibraryDocumentType.OwnerManual]),
        new(AccessoriesSectionSlug, "Аксессуары и прочее", "Accessories", false,
            [TelegramLibraryDocumentType.OwnerManual, TelegramLibraryDocumentType.ControllerGuide])
    ];

    private static readonly IReadOnlyList<ManualDocumentTypeOption> DocumentTypeOptions =
    [
        new("service", TelegramLibraryDocumentType.ServiceManual, "📕 Сервисные мануалы"),
        new("owner", TelegramLibraryDocumentType.OwnerManual, "📘 Руководства пользователя"),
        new("installation", TelegramLibraryDocumentType.InstallationManual, "🛠 Installation Manual"),
        new("controller", TelegramLibraryDocumentType.ControllerGuide, "🎛 Controller Guide")
    ];

    private static readonly IReadOnlyList<TypedLibraryCategoryOption> IndoorCategories =
    [
        new("wall", "Настенные"),
        new("cas", "Кассетные"),
        new("duc", "Канальные"),
        new("svc", "📕 Сервисные мануалы")
    ];

    private static readonly IReadOnlyList<TypedLibraryCategoryOption> ControllerCategories =
    [
        new("wall", "Настенные"),
        new("ir", "Беспроводные ИК")
    ];

    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly ITelegramDiagnosticCaseStore _historyStore;
    private readonly IErrorKnowledgeLocalizationSource _localizedKnowledge;
    private readonly ITelegramManualRegistrySource _manualRegistry;
    private readonly ITelegramManualFileBindingStore _bindingStore;
    private readonly ITelegramUserStore _userStore;
    private readonly ITelegramLibraryAccessStore _libraryAccessStore;
    private readonly IEquipmentDiagnosticTelegramOutboundClient _outboundClient;
    private readonly ILogger<TelegramManualLibraryService> _logger;
    private readonly ConcurrentDictionary<long, ManualBindSession> _manualBindSessions = new();

    public TelegramManualLibraryService(
        EquipmentDiagnosticTelegramOptions options,
        ITelegramDiagnosticCaseStore historyStore,
        IErrorKnowledgeLocalizationSource localizedKnowledge,
        ITelegramManualRegistrySource manualRegistry,
        ITelegramManualFileBindingStore bindingStore,
        ITelegramUserStore userStore,
        ITelegramLibraryAccessStore libraryAccessStore,
        IEquipmentDiagnosticTelegramOutboundClient outboundClient,
        ILogger<TelegramManualLibraryService>? logger = null)
    {
        _options = options;
        _historyStore = historyStore;
        _localizedKnowledge = localizedKnowledge;
        _manualRegistry = manualRegistry;
        _bindingStore = bindingStore;
        _userStore = userStore;
        _libraryAccessStore = libraryAccessStore;
        _outboundClient = outboundClient;
        _logger = logger ?? NullLogger<TelegramManualLibraryService>.Instance;
    }

    public static bool IsManualRequest(string? text) =>
        string.Equals(text?.Trim(), ManualRequestCommand, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(text?.Trim(), ManualLibraryButton, StringComparison.Ordinal);

    public static bool IsLibraryRequest(string? text) =>
        string.Equals(text?.Trim(), LibraryCommand, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(text?.Trim(), LibraryButton, StringComparison.Ordinal) ||
        text?.TrimStart().StartsWith("/library_grant ", StringComparison.OrdinalIgnoreCase) == true ||
        text?.TrimStart().StartsWith("/library_revoke ", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsDiagnosticManualRequest(string? text) =>
        string.Equals(text?.Trim(), DiagnosticGuideButton, StringComparison.Ordinal) ||
        string.Equals(text?.Trim(), DiagnosticManualButton, StringComparison.Ordinal);

    public static bool IsDiagnosticManualCallback(string? callbackData) =>
        string.Equals(callbackData, DiagnosticManualCallbackData, StringComparison.Ordinal) ||
        callbackData?.StartsWith(DiagnosticManualFilePrefix, StringComparison.Ordinal) == true;

    public static bool IsManualRegistration(string? text) =>
        text?.TrimStart().StartsWith(ManualRegisterCommand, StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsManualUnregistration(string? text) =>
        text?.TrimStart().StartsWith(ManualUnregisterCommand, StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsManualBindingList(string? text) =>
        string.Equals(text?.Trim(), ManualBindingsCommand, StringComparison.OrdinalIgnoreCase);

    public static bool IsManualBindCommand(string? text) =>
        string.Equals(text?.Trim(), ManualBindCommand, StringComparison.OrdinalIgnoreCase);

    public static bool IsManualBindCallback(string? callbackData) =>
        callbackData?.StartsWith(ManualBindCallbackPrefix, StringComparison.Ordinal) == true;

    public static bool IsLibraryCallback(string? callbackData) =>
        callbackData?.StartsWith(LibraryCallbackPrefix, StringComparison.Ordinal) == true;

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
                DocumentFileName: binding.OriginalFileName,
                ProtectContent: true));
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

    public async Task<TelegramManualLibraryResult> RequestDiagnosticGuideAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (update.CallbackData?.StartsWith(DiagnosticManualFilePrefix, StringComparison.Ordinal) == true)
        {
            return await SendDiagnosticOwnerManualFileAsync(
                update.CallbackData[DiagnosticManualFilePrefix.Length..],
                access,
                cancellationToken);
        }

        return await RequestDiagnosticGuideAsync(access, cancellationToken);
    }

    public async Task<TelegramManualLibraryResult> RequestDiagnosticGuideAsync(
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return ManualUnavailable("Библиотека руководств сейчас выключена.");
        }

        if (!IsActiveUser(access))
        {
            return ManualUnavailable("Доступ ограничен.");
        }

        if (access.User is null)
        {
            return MissingDiagnosticContext();
        }

        var last = await _historyStore.GetLastForTelegramUserAsync(access.User.Id, cancellationToken);
        var series = last is null ? null : DiagnosticSeries(last);
        if (last is null ||
            last.Status != TelegramDiagnosticCaseStatus.Completed ||
            string.IsNullOrWhiteSpace(last.Manufacturer) ||
            !string.Equals(last.Manufacturer, "Gree", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(last.Code) ||
            string.IsNullOrWhiteSpace(series))
        {
            return MissingDiagnosticContext();
        }

        var equipment = $"{last.Manufacturer} {series}";
        var bindings = await ListDiagnosticOwnerManualsAsync(series, cancellationToken);
        if (bindings.Length == 0)
        {
            return Html(
                $"{TelegramHtml.Bold("Руководство пока не добавлено")}\n\n" +
                $"Для {TelegramHtml.Escape(equipment)} / {TelegramHtml.Escape(last.Code)} есть диагностическая карточка, " +
                "но безопасное Owner/User руководство для диагностики пока не привязано.",
                callbackAnswerText: "Руководство не добавлено",
                replyMarkup: TelegramDiagnosticConversationService.DiagnosticManualContextKeyboard(access));
        }

        if (bindings.Length > 1)
        {
            return BuildDiagnosticOwnerManualSelection(last, series, bindings);
        }

        var replyMarkup = TelegramDiagnosticConversationService.DiagnosticManualContextKeyboard(access);
        var binding = bindings[0];
        var heading =
            $"{TelegramHtml.Bold("Руководство по диагностике")}\n\n" +
            $"{TelegramHtml.Bold("Оборудование:")} {TelegramHtml.Escape(equipment)}\n" +
            $"{TelegramHtml.Bold("Код:")} {TelegramHtml.Escape(last.Code)}\n\n" +
            "Отправляю сохранённое Owner/User руководство для этой серии.";
        var messages = new List<EquipmentDiagnosticTelegramOutboundMessage>
        {
            new(
                heading,
                ParseMode: TelegramHtml.ParseMode,
                ReplyMarkup: replyMarkup),
            new(
                "Сохранённое руководство по последней диагностике.",
                ReplyMarkup: replyMarkup,
                DocumentFileId: binding.TelegramFileId,
                DocumentFileName: binding.OriginalFileName,
                ProtectContent: true)
        };

        return new TelegramManualLibraryResult(
            heading,
            [],
            messages,
            TelegramHtml.ParseMode,
            "Отправляю руководство",
            replyMarkup);
    }

    public async Task<TelegramManualLibraryResult> RequestDiagnosticManualAsync(
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!access.CanAccessDiagnosticManual)
        {
            return Html(
                $"{TelegramHtml.Bold("Доступ ограничен")}\n\n" +
                "Мануалы доступны только для технических ролей.",
                callbackAnswerText: "Доступ ограничен");
        }

        if (!_options.ManualLibrary.Enabled)
        {
            return ManualUnavailable("Библиотека мануалов сейчас выключена.");
        }

        if (access.User is null)
        {
            return MissingDiagnosticContext();
        }

        var last = await _historyStore.GetLastForTelegramUserAsync(access.User.Id, cancellationToken);
        var series = last is null ? null : DiagnosticSeries(last);
        if (last is null ||
            last.Status != TelegramDiagnosticCaseStatus.Completed ||
            string.IsNullOrWhiteSpace(last.Manufacturer) ||
            !string.Equals(last.Manufacturer, "Gree", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(last.Code) ||
            string.IsNullOrWhiteSpace(series))
        {
            return MissingDiagnosticContext();
        }

        var replyMarkup = TelegramDiagnosticConversationService.DiagnosticManualContextKeyboard(access);
        var equipment = $"{last.Manufacturer} {series}";
        var binding = await _bindingStore.GetBySeriesAsync(BrandGree, series, cancellationToken);
        if (binding is null)
        {
            return Html(
                $"{TelegramHtml.Bold("Мануал пока не привязан")}\n\n" +
                $"Для {TelegramHtml.Escape(equipment)} / {TelegramHtml.Escape(last.Code)} есть диагностическая карточка, " +
                "но файл мануала ещё отсутствует.",
                callbackAnswerText: "Мануал не привязан",
                replyMarkup: replyMarkup);
        }

        var heading =
            $"{TelegramHtml.Bold("Мануал по диагностике")}\n\n" +
            $"{TelegramHtml.Bold("Оборудование:")} {TelegramHtml.Escape(equipment)}\n" +
            $"{TelegramHtml.Bold("Код:")} {TelegramHtml.Escape(last.Code)}\n\n" +
            "Отправляю сохранённый мануал для этой серии.";
        var boundManuals = new[] { binding };
        var messages = new List<EquipmentDiagnosticTelegramOutboundMessage>
        {
            new(
                heading,
                ParseMode: TelegramHtml.ParseMode,
                ReplyMarkup: replyMarkup)
        };
        messages.AddRange(boundManuals.Select(binding =>
            new EquipmentDiagnosticTelegramOutboundMessage(
                "Сохранённый мануал по последней диагностике.",
                ReplyMarkup: replyMarkup,
                DocumentFileId: binding.TelegramFileId,
                DocumentFileName: binding.OriginalFileName,
                ProtectContent: true)));

        return new TelegramManualLibraryResult(
            heading,
            [],
            messages,
            TelegramHtml.ParseMode,
            "Отправляю мануал",
            replyMarkup);
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

        var parts = SplitCommand(update.Text);
        if (parts.Length < 2)
        {
            return Text("Укажите manualId из реестра: /manual_register <manualId>. Файл должен быть приложен документом Telegram.");
        }

        if (parts.Length > 2)
        {
            return Text("Идентификатор файла не принимается текстом. Отправьте документ с подписью /manual_register <manualId> или ответьте командой на документ.");
        }

        var manual = FindManual(parts[1]);
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

    public async Task<TelegramManualLibraryResult> UnregisterManualAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return Text("Библиотека руководств сейчас выключена.");
        }

        if (!CanManageManuals(access.Role))
        {
            return Text("Управление файлами руководств доступно только админу или владельцу.");
        }

        var parts = SplitCommand(update.Text);
        if (parts.Length != 2)
        {
            return Text("Укажите manualId из реестра: /manual_unregister <manualId>.");
        }

        var manual = FindManual(parts[1]);
        if (manual is null)
        {
            return Text("Руководство с таким идентификатором не найдено в реестре.");
        }

        var removed = await _bindingStore.RemoveAsync(manual.ManualId, cancellationToken);
        return Text(removed
            ? $"Файл руководства отключен: {DisplayName(manual)}."
            : $"Для этого руководства файл не был подключен: {DisplayName(manual)}.");
    }

    public async Task<TelegramManualLibraryResult> ListBindingsAsync(
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return Text("Библиотека руководств сейчас выключена.");
        }

        if (!CanManageManuals(access.Role))
        {
            return Text("Список подключенных руководств доступен только админу или владельцу.");
        }

        var bindings = (await _bindingStore.ListAsync(cancellationToken))
            .ToDictionary(binding => binding.ManualId, StringComparer.OrdinalIgnoreCase);
        var manuals = _manualRegistry.GetManuals()
            .Where(manual => manual.EligibleForTelegramLibrary)
            .OrderBy(DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (manuals.Length == 0)
        {
            return Text("В реестре пока нет руководств, разрешенных для Telegram-библиотеки.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("Подключение руководств");
        foreach (var manual in manuals)
        {
            builder.Append("- ");
            builder.Append(DisplayName(manual));
            if (bindings.TryGetValue(manual.ManualId, out var binding))
            {
                builder.Append(": подключено");
                var safeName = SafeFileName(binding.OriginalFileName);
                if (!string.IsNullOrWhiteSpace(safeName))
                {
                    builder.Append($"; файл: {safeName}");
                }
            }
            else
            {
                builder.Append(": файл не подключен");
            }

            builder.AppendLine();
        }

        return Text(builder.ToString().Trim());
    }

    public async Task<TelegramManualLibraryResult> OpenLibraryAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return Text("Библиотека файлов сейчас выключена.");
        }

        var text = update.Text?.Trim() ?? string.Empty;
        if (text.StartsWith("/library_grant ", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("/library_revoke ", StringComparison.OrdinalIgnoreCase))
        {
            return await HandleLibraryAccessCommandAsync(text, access, cancellationToken);
        }

        if (!IsActiveUser(access))
        {
            return Text("Доступ ограничен.");
        }

        if (access.Role == TelegramUserRole.Consumer)
        {
            return Text("Раздел библиотеки недоступен для вашей роли.");
        }

        if (!access.CanAccessLibrary)
        {
            return BindText(
                "Доступ к библиотеке файлов не выдан.\n\nВы можете отправить запрос владельцу.",
                AccessRequestKeyboard());
        }

        return BuildLibraryHome(access);
    }

    public async Task<TelegramManualLibraryResult> HandleLibraryCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return Text("Библиотека файлов сейчас выключена.");
        }

        if (!IsActiveUser(access))
        {
            return BindText("Доступ ограничен.", callbackAnswerText: "Доступ ограничен");
        }

        var callback = update.CallbackData ?? string.Empty;
        if (string.Equals(callback, LibraryRequestAccessCallback, StringComparison.Ordinal))
        {
            if (!TelegramUserRolePolicy.CanRequestTelegramLibraryAccess(access.Role) || access.User is null)
            {
                return BindText("Раздел библиотеки недоступен для вашей роли.", callbackAnswerText: "Недоступно");
            }

            var request = await _libraryAccessStore.CreateOrGetPendingRequestAsync(access.User, cancellationToken: cancellationToken);
            return BindText(
                $"Запрос доступа к библиотеке отправлен владельцу. Номер запроса: {request.Id}.",
                callbackAnswerText: "Запрос отправлен");
        }

        if (callback.StartsWith(LibraryApprovePrefix, StringComparison.Ordinal) ||
            callback.StartsWith(LibraryRejectPrefix, StringComparison.Ordinal))
        {
            return await ResolveLibraryAccessRequestAsync(callback, access, cancellationToken);
        }

        if (callback.StartsWith(LibraryGrantPrefix, StringComparison.Ordinal) ||
            callback.StartsWith(LibraryRevokePrefix, StringComparison.Ordinal))
        {
            return await HandleLibraryAccessCallbackAsync(callback, access, cancellationToken);
        }

        if (!access.CanAccessLibrary)
        {
            return BindText("Доступ к библиотеке файлов не выдан.", AccessRequestKeyboard(), "Нет доступа");
        }

        if (string.Equals(callback, LibraryOpenCallback, StringComparison.Ordinal))
        {
            return BuildLibraryHome(access);
        }

        if (string.Equals(callback, LibraryGreeCallback, StringComparison.Ordinal))
        {
            return BuildGreeCatalog();
        }

        if (string.Equals(callback, LibraryRemotesCallback, StringComparison.Ordinal))
        {
            return await BuildFreeLibrarySectionAsync(FindSection(ControllersSectionSlug)!, access, 0, cancellationToken);
        }

        if (string.Equals(callback, LibraryGreeOutdoorCallback, StringComparison.Ordinal))
        {
            return BuildOutdoorProductLines();
        }

        if (callback.StartsWith(LibraryGreeProductPrefix, StringComparison.Ordinal))
        {
            var productRemainder = callback[LibraryGreeProductPrefix.Length..];
            var separator = productRemainder.IndexOf(':', StringComparison.Ordinal);
            var productSlug = separator < 0 ? productRemainder : productRemainder[..separator];
            var series = FindSeries(productSlug);
            if (series is null)
            {
                return BindText("Раздел не найден.", callbackAnswerText: "Не найдено");
            }

            if (separator < 0)
            {
                return BuildOutdoorDocumentBuckets(series);
            }

            var documentType = FindDocumentType(productRemainder[(separator + 1)..]);
            if (documentType is null)
            {
                return BindText("Тип документа не найден.", callbackAnswerText: "Не найдено");
            }

            if (!IsVisibleLibraryDocumentType(documentType.DocumentType))
            {
                return BuildOutdoorDocumentBuckets(series);
            }

            return await BuildOutdoorBucketAsync(series, documentType, access, cancellationToken);
        }

        if (callback.StartsWith(LibraryGreeSectionPrefix, StringComparison.Ordinal))
        {
            var sectionRemainder = callback[LibraryGreeSectionPrefix.Length..];
            var sectionSeparator = sectionRemainder.IndexOf(':', StringComparison.Ordinal);
            var sectionSlug = sectionSeparator < 0 ? sectionRemainder : sectionRemainder[..sectionSeparator];
            var documentSlug = sectionSeparator < 0 ? null : sectionRemainder[(sectionSeparator + 1)..];
            var page = 0;
            var pageSeparator = (documentSlug ?? sectionSlug).IndexOf(":p:", StringComparison.Ordinal);
            if (pageSeparator >= 0)
            {
                if (documentSlug is null)
                {
                    var pageText = sectionSlug[(pageSeparator + 3)..];
                    sectionSlug = sectionSlug[..pageSeparator];
                    _ = int.TryParse(pageText, out page);
                }
                else
                {
                    var pageText = documentSlug[(pageSeparator + 3)..];
                    documentSlug = documentSlug[..pageSeparator];
                    _ = int.TryParse(pageText, out page);
                }

                page = Math.Max(0, page);
            }

            var section = FindSection(sectionSlug);
            if (section is null || section.IsOutdoor)
            {
                return BindText("Раздел не найден.", callbackAnswerText: "Не найдено");
            }

            if (section.Slug.Equals(IndoorSectionSlug, StringComparison.Ordinal))
            {
                return BuildIndoorCategoryRoot();
            }

            if (section.Slug.Equals(ControllersSectionSlug, StringComparison.Ordinal))
            {
                return BuildControllerCategoryRoot();
            }

            if (section.Slug.Equals(UMatchSectionSlug, StringComparison.Ordinal) ||
                section.Slug.Equals(ErvSectionSlug, StringComparison.Ordinal))
            {
                if (documentSlug is null)
                {
                    return BuildSectionDocumentBuckets(section);
                }

                var documentType = FindDocumentType(documentSlug);
                if (documentType is null || !section.DocumentTypes.Contains(documentType.DocumentType))
                {
                    return BuildSectionDocumentBuckets(section);
                }

                return await BuildSectionBucketAsync(section, documentType, access, page, cancellationToken);
            }

            return await BuildFreeLibrarySectionAsync(section, access, page, cancellationToken);
        }

        if (callback.StartsWith(LibraryIndoorCategoryPrefix, StringComparison.Ordinal))
        {
            return await BuildTypedCategoryAsync(
                callback[LibraryIndoorCategoryPrefix.Length..],
                IndoorSectionSlug,
                "Внутренние блоки Gree",
                LibraryIndoorCategoryPrefix,
                FreeSectionCallback(IndoorSectionSlug),
                access,
                cancellationToken);
        }

        if (callback.StartsWith(LibraryControllerCategoryPrefix, StringComparison.Ordinal))
        {
            return await BuildTypedCategoryAsync(
                callback[LibraryControllerCategoryPrefix.Length..],
                ControllersSectionSlug,
                "Пульты / Controllers Gree",
                LibraryControllerCategoryPrefix,
                FreeSectionCallback(ControllersSectionSlug),
                access,
                cancellationToken);
        }

        if (string.Equals(callback, LibraryRequestsCallback, StringComparison.Ordinal))
        {
            return await BuildAccessRequestsAsync(access, cancellationToken);
        }

        if (string.Equals(callback, LibraryAccessCallback, StringComparison.Ordinal))
        {
            return await BuildAccessManagementAsync(access, cancellationToken);
        }

        if (string.Equals(callback, LibraryCancelCallback, StringComparison.Ordinal))
        {
            return BindText(
                "Библиотека закрыта.",
                TelegramDiagnosticConversationService.MainKeyboard(access),
                "Закрыто");
        }

        if (callback.StartsWith(LibraryFileByIdPrefix, StringComparison.Ordinal))
        {
            return await SendLibraryFileByIdAsync(callback[LibraryFileByIdPrefix.Length..], access, cancellationToken);
        }

        if (callback.StartsWith(LibraryFilePrefix, StringComparison.Ordinal))
        {
            return await SendLibraryFileAsync(callback[LibraryFilePrefix.Length..], access, cancellationToken);
        }

        return BindText("Действие библиотеки устарело.", callbackAnswerText: "Действие устарело");
    }

    public Task<TelegramManualLibraryResult> StartManualBindAsync(
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_options.ManualLibrary.Enabled)
        {
            return Task.FromResult(Text("Библиотека руководств сейчас выключена."));
        }

        if (!CanManageManuals(access.Role) || access.User is null)
        {
            return Task.FromResult(Text("Привязка мануалов доступна только администратору или владельцу."));
        }

        _manualBindSessions[access.User.Id] = new ManualBindSession(
            ManualBindStage.SelectingBrand,
            null,
            null,
            null,
            null);

        return Task.FromResult(BindText(
            "Выберите бренд для добавления защищенного PDF-файла.",
            BrandKeyboard()));
    }

    public async Task<TelegramManualLibraryResult> HandleManualBindCallbackAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return Text("Библиотека руководств сейчас выключена.");
        }

        if (!CanManageManuals(access.Role) || access.User is null)
        {
            return Text("Привязка мануалов доступна только администратору или владельцу.");
        }

        if (string.Equals(update.CallbackData, ManualBindStart, StringComparison.Ordinal))
        {
            return await StartManualBindAsync(access, cancellationToken);
        }

        if (string.Equals(update.CallbackData, ManualBindCancel, StringComparison.Ordinal))
        {
            _manualBindSessions.TryRemove(access.User.Id, out _);
            return BindText("Привязка мануала отменена.", callbackAnswerText: "Отменено");
        }

        if (update.CallbackData?.StartsWith(ManualBindBrandPrefix, StringComparison.Ordinal) == true)
        {
            var brand = update.CallbackData[ManualBindBrandPrefix.Length..];
            if (!brand.Equals("gree", StringComparison.Ordinal))
            {
                return BindText("Бренд не распознан.", BrandKeyboard(), "Выберите бренд");
            }

            _manualBindSessions[access.User.Id] = new ManualBindSession(
                ManualBindStage.SelectingSection,
                null,
                null,
                null,
                null);
            return BindText(
                "Gree\n\nВыберите раздел для файла.",
                SectionKeyboard(),
                "Выберите раздел");
        }

        if (update.CallbackData?.StartsWith(ManualBindSectionPrefix, StringComparison.Ordinal) == true)
        {
            var slug = update.CallbackData[ManualBindSectionPrefix.Length..];
            var section = FindSection(slug);
            if (section is null)
            {
                return BindText("Раздел не распознан.", SectionKeyboard(), "Выберите раздел");
            }

            _manualBindSessions[access.User.Id] = new ManualBindSession(
                section.IsOutdoor ? ManualBindStage.SelectingSeries : ManualBindStage.SelectingDocumentType,
                section,
                null,
                null,
                null);
            return section.IsOutdoor
                ? BindText(
                    "Gree / Наружные\n\nВыберите продуктовую линейку.",
                    SeriesKeyboard(),
                    "Выберите линейку")
                : BindText(
                    $"Gree / {section.DisplayName}\n\nВыберите тип документа.",
                    DocumentTypeKeyboard(section.DocumentTypes),
                    "Выберите тип");
        }

        if (update.CallbackData?.StartsWith(ManualBindSeriesPrefix, StringComparison.Ordinal) == true)
        {
            var slug = update.CallbackData[ManualBindSeriesPrefix.Length..];
            var series = FindSeries(slug);
            if (series is null)
            {
                return BindText(
                    "Серия не распознана. Выберите серию из списка.",
                    SeriesKeyboard(),
                    "Выберите серию");
            }

            var session = _manualBindSessions.TryGetValue(access.User.Id, out var current)
                ? current
                : new ManualBindSession(ManualBindStage.SelectingSeries, FindSection(OutdoorSectionSlug), null, null, null);
            var section = session.Section ?? FindSection(OutdoorSectionSlug);
            _manualBindSessions[access.User.Id] = session with
            {
                Stage = ManualBindStage.SelectingDocumentType,
                Section = section,
                Series = series,
                DocumentType = null,
                Candidate = null
            };
            return BindText(
                $"Gree / Наружные / {series.DisplayName}\n\nВыберите тип документа.",
                DocumentTypeKeyboard(section?.DocumentTypes ?? OutdoorDocumentTypes().Select(item => item.DocumentType)),
                "Выберите тип");
        }

        if (update.CallbackData?.StartsWith(ManualBindDocumentTypePrefix, StringComparison.Ordinal) == true)
        {
            if (!_manualBindSessions.TryGetValue(access.User.Id, out var session) ||
                session.Section is null)
            {
                return BindText(
                    "Сессия привязки устарела. Запустите /manual_bind заново.",
                    callbackAnswerText: "Сессия устарела");
            }

            var documentType = FindDocumentType(update.CallbackData[ManualBindDocumentTypePrefix.Length..]);
            if (documentType is null || !session.Section.DocumentTypes.Contains(documentType.DocumentType))
            {
                return BindText(
                    "Тип документа не распознан. Выберите тип из списка.",
                    DocumentTypeKeyboard(session.Section.DocumentTypes),
                    "Выберите тип");
            }

            if (session.Section.IsOutdoor && session.Series is null)
            {
                return BindText(
                    "Выберите продуктовую линейку Gree.",
                    SeriesKeyboard(),
                    "Выберите линейку");
            }

            _manualBindSessions[access.User.Id] = session with
            {
                Stage = ManualBindStage.WaitingForDocument,
                DocumentType = documentType,
                Candidate = null
            };
            return BindText(
                BuildManualBindDocumentPrompt(session.Section, session.Series, documentType),
                CancelKeyboard(),
                "Пришлите PDF");
        }

        if (update.CallbackData is ManualBindConfirm or ManualBindReplace)
        {
            if (!_manualBindSessions.TryGetValue(access.User.Id, out var session) ||
                session.Section is null ||
                session.DocumentType is null ||
                session.Candidate is null)
            {
                return BindText(
                    "Сессия привязки устарела. Запустите /manual_bind заново.",
                    callbackAnswerText: "Сессия устарела");
            }

            var binding = CreateManualBinding(session, access, update);
            if (UsesManualIdStorage(session))
            {
                await _bindingStore.UpsertAsync(binding, cancellationToken);
            }
            else
            {
                await _bindingStore.UpsertSeriesAsync(binding, cancellationToken);
            }

            _manualBindSessions.TryRemove(access.User.Id, out _);

            return BindText(
                $"Файл добавлен: {BuildManualBindTargetLabel(session)}. Файл: {session.Candidate.FileName}.",
                callbackAnswerText: "Мануал привязан");
        }

        return BindText(
            "Действие привязки не распознано. Запустите /manual_bind заново.",
            callbackAnswerText: "Действие устарело");
    }

    public async Task<TelegramManualLibraryResult?> TryContinueManualBindAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken = default)
    {
        if (!_options.ManualLibrary.Enabled ||
            !CanManageManuals(access.Role) ||
            access.User is null ||
            !_manualBindSessions.TryGetValue(access.User.Id, out var session))
        {
            return null;
        }

        if (string.Equals(update.Text?.Trim(), TelegramDiagnosticConversationService.CancelButton, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(update.Text?.Trim(), "cancel", StringComparison.OrdinalIgnoreCase))
        {
            _manualBindSessions.TryRemove(access.User.Id, out _);
            return BindText("Привязка мануала отменена.");
        }

        if (session.Section is null)
        {
            return BindText(
                "Выберите раздел Gree для добавления защищенного PDF-файла.",
                SectionKeyboard());
        }

        if (session.Section.IsOutdoor && session.Series is null)
        {
            return BindText(
                "Выберите продуктовую линейку Gree для добавления защищенного PDF-файла.",
                SeriesKeyboard());
        }

        if (session.DocumentType is null)
        {
            return BindText(
                "Выберите тип документа.",
                DocumentTypeKeyboard(session.Section.DocumentTypes));
        }

        var document = GetRegistrationDocument(update);
        if (document is null)
        {
            return BindText("Ожидаю PDF-файл.", CancelKeyboard());
        }

        if (!IsValidManualBindDocument(document.Value, out var reason))
        {
            return BindText(
                $"{reason}\n\nРекомендуемое имя: {RecommendedFileName(session)}",
                CancelKeyboard());
        }

        var candidate = new ManualBindCandidate(
            document.Value.FileId,
            document.Value.FileUniqueId,
            SafeFileName(document.Value.FileName) ?? RecommendedFileName(session),
            SafeContentType(document.Value.MimeType),
            document.Value.FileSize,
            update.UserId ?? access.User.TelegramUserId,
            update.ChatId);
        var candidateSession = session with { Candidate = candidate };
        var existing = await FindExistingManualBindingAsync(candidateSession, cancellationToken);
        var nextStage = existing is null
            ? ManualBindStage.WaitingForConfirmation
            : ManualBindStage.WaitingForReplaceConfirmation;

        _manualBindSessions[access.User.Id] = candidateSession with
        {
            Stage = nextStage
        };

        var message = existing is null
            ? $"Подтвердите добавление {BuildManualBindTargetLabel(candidateSession)}: {candidate.FileName}."
            : $"Для {BuildManualBindTargetLabel(candidateSession)} уже есть активный файл: {SafeFileName(existing.OriginalFileName) ?? "без имени"}. Заменить на {candidate.FileName}?";

        return BindText(
            message,
            ConfirmKeyboard(existing is not null));
    }

    private async Task<TelegramManualLibraryResult> HandleLibraryAccessCommandAsync(
        string text,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (!CanManageLibrary(access.Role) || access.User is null)
        {
            return Text("Управление доступом к библиотеке доступно только владельцу.");
        }

        var parts = SplitCommand(text);
        if (parts.Length != 2 || !long.TryParse(parts[1], out var chatId))
        {
            return Text("Команда ожидает chatId: /library_grant <chatId> или /library_revoke <chatId>.");
        }

        var user = await _userStore.GetByChatIdAsync(chatId, cancellationToken);
        if (user is null)
        {
            return Text("Пользователь не найден.");
        }

        if (!CanGrantLibraryTo(user))
        {
            return Text("Доступ к библиотеке можно выдать только активному Admin/Engineer/Installer.");
        }

        var notificationSent = true;
        if (text.StartsWith("/library_grant", StringComparison.OrdinalIgnoreCase))
        {
            await _libraryAccessStore.GrantAsync(user.Id, access.User.Id, "Owner grant", cancellationToken);
            notificationSent = await NotifyLibraryAccessChangedAsync(user, granted: true, cancellationToken);
            return Text($"Доступ к библиотеке выдан: {SafeUserLabel(user)}.{NotificationStatusText(notificationSent)}");
        }

        var revoked = await _libraryAccessStore.RevokeAsync(user.Id, access.User.Id, cancellationToken);
        if (revoked)
        {
            notificationSent = await NotifyLibraryAccessChangedAsync(user, granted: false, cancellationToken);
        }

        return Text(revoked
            ? $"Доступ к библиотеке отозван: {SafeUserLabel(user)}.{NotificationStatusText(notificationSent)}"
            : $"Активный доступ к библиотеке не найден: {SafeUserLabel(user)}.");
    }

    private async Task<TelegramManualLibraryResult> HandleLibraryAccessCallbackAsync(
        string callback,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (!CanManageLibrary(access.Role) || access.User is null)
        {
            return BindText("Управление доступом к библиотеке доступно только владельцу.", callbackAnswerText: "Нет доступа");
        }

        var grant = callback.StartsWith(LibraryGrantPrefix, StringComparison.Ordinal);
        var prefix = grant ? LibraryGrantPrefix : LibraryRevokePrefix;
        if (!long.TryParse(callback[prefix.Length..], out var userId))
        {
            return BindText("Пользователь не найден.", callbackAnswerText: "Ошибка");
        }

        var user = await _userStore.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return BindText("Пользователь не найден.", callbackAnswerText: "Не найден");
        }

        var notificationSent = true;
        if (grant)
        {
            if (!CanGrantLibraryTo(user))
            {
                return BindText("Доступ к библиотеке можно выдать только активному Admin/Engineer/Installer.", callbackAnswerText: "Нельзя выдать");
            }

            await _libraryAccessStore.GrantAsync(user.Id, access.User.Id, "Owner grant", cancellationToken);
            notificationSent = await NotifyLibraryAccessChangedAsync(user, granted: true, cancellationToken);
            return BindText($"Доступ к библиотеке выдан: {SafeUserLabel(user)}.{NotificationStatusText(notificationSent)}", callbackAnswerText: "Доступ выдан");
        }

        var revoked = await _libraryAccessStore.RevokeAsync(user.Id, access.User.Id, cancellationToken);
        if (revoked)
        {
            notificationSent = await NotifyLibraryAccessChangedAsync(user, granted: false, cancellationToken);
        }

        return BindText(revoked ? $"Доступ к библиотеке отозван.{NotificationStatusText(notificationSent)}" : "Активный доступ уже отсутствует.", callbackAnswerText: "Готово");
    }

    private async Task<TelegramManualLibraryResult> ResolveLibraryAccessRequestAsync(
        string callback,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (!CanManageLibrary(access.Role) || access.User is null)
        {
            return BindText("Управление доступом к библиотеке доступно только владельцу.", callbackAnswerText: "Нет доступа");
        }

        var approve = callback.StartsWith(LibraryApprovePrefix, StringComparison.Ordinal);
        var prefix = approve ? LibraryApprovePrefix : LibraryRejectPrefix;
        if (!long.TryParse(callback[prefix.Length..], out var requestId))
        {
            return BindText("Запрос не найден.", callbackAnswerText: "Ошибка");
        }

        var request = await _libraryAccessStore.GetRequestAsync(requestId, cancellationToken);
        if (request is null || request.Status != TelegramLibraryAccessRequestStatus.Pending)
        {
            return BindText("Запрос уже обработан или устарел.", callbackAnswerText: "Устарело");
        }

        var user = await _userStore.GetByIdAsync(request.TelegramUserId, cancellationToken);
        if (user is null || !CanGrantLibraryTo(user))
        {
            await _libraryAccessStore.ResolveRequestAsync(
                requestId,
                TelegramLibraryAccessRequestStatus.Rejected,
                access.User.Id,
                cancellationToken);
            return BindText("Запрос отклонён: пользователь не подходит для доступа к библиотеке.", callbackAnswerText: "Отклонено");
        }

        var status = approve
            ? TelegramLibraryAccessRequestStatus.Approved
            : TelegramLibraryAccessRequestStatus.Rejected;
        await _libraryAccessStore.ResolveRequestAsync(requestId, status, access.User.Id, cancellationToken);
        if (approve)
        {
            await _libraryAccessStore.GrantAsync(user.Id, access.User.Id, "Approved access request", cancellationToken);
        }

        var notificationSent = await NotifyAccessRequestResolvedAsync(user, approve, cancellationToken);

        return BindText(
            approve
                ? $"Запрос одобрен. Доступ выдан: {SafeUserLabel(user)}.{NotificationStatusText(notificationSent)}"
                : $"Запрос отклонён: {SafeUserLabel(user)}.{NotificationStatusText(notificationSent)}",
            callbackAnswerText: approve ? "Одобрено" : "Отклонено");
    }

    private TelegramManualLibraryResult BuildLibraryHome(TelegramUserAccessResult access)
    {
        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>();
        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Gree", LibraryGreeCallback)]);

        if (CanManageLibrary(access.Role))
        {
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("➕ Добавить файл", ManualBindStart)]);
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Запросы доступа", LibraryRequestsCallback)]);
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Управление доступом", LibraryAccessCallback)]);
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton(TelegramUserOverviewService.UsersButton, TelegramUserOverviewService.UsersOverviewCallback)]);
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton(TelegramBroadcastService.BroadcastButton, TelegramBroadcastService.BroadcastMenuCallback)]);
        }

        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", LibraryCancelCallback)]);
        return BindText(
            "📚 Библиотека файлов\n\nВыберите раздел:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Библиотека");
    }

    private static TelegramManualLibraryResult BuildGreeCatalog()
    {
        var rows = new[]
        {
            new[] { new EquipmentDiagnosticTelegramInlineKeyboardButton("Наружные", LibraryGreeOutdoorCallback) },
            [new EquipmentDiagnosticTelegramInlineKeyboardButton("Внутренние", FreeSectionCallback(IndoorSectionSlug))],
            [new EquipmentDiagnosticTelegramInlineKeyboardButton("Пульты / Controllers", FreeSectionCallback(ControllersSectionSlug))],
            [new EquipmentDiagnosticTelegramInlineKeyboardButton("Полупром / U-Match", FreeSectionCallback(UMatchSectionSlug))],
            [new EquipmentDiagnosticTelegramInlineKeyboardButton("Вентиляция ERV", FreeSectionCallback(ErvSectionSlug))],
            [new EquipmentDiagnosticTelegramInlineKeyboardButton("Аксессуары и прочее", FreeSectionCallback(AccessoriesSectionSlug))],
            [new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)]
        };

        return BindText(
            "Gree\n\nВыберите раздел:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Gree");
    }

    private static TelegramManualLibraryResult BuildIndoorCategoryRoot()
    {
        var rows = IndoorCategories
            .Select(category => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton(category.Label, LibraryIndoorCategoryPrefix + category.Slug)])
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryGreeCallback)])
            .ToArray();

        return BindText(
            "Внутренние блоки Gree",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Внутренние");
    }

    private static TelegramManualLibraryResult BuildControllerCategoryRoot()
    {
        var rows = ControllerCategories
            .Select(category => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton(category.Label, LibraryControllerCategoryPrefix + category.Slug)])
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryGreeCallback)])
            .ToArray();

        return BindText(
            "Пульты / Controllers Gree",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Пульты");
    }

    private async Task<TelegramManualLibraryResult> BuildTypedCategoryAsync(
        string callbackRemainder,
        string sectionSlug,
        string sectionTitle,
        string callbackPrefix,
        string backCallback,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var categorySlug = callbackRemainder;
        var page = 0;
        var pageSeparator = callbackRemainder.IndexOf(":p:", StringComparison.Ordinal);
        if (pageSeparator >= 0)
        {
            categorySlug = callbackRemainder[..pageSeparator];
            _ = int.TryParse(callbackRemainder[(pageSeparator + 3)..], out page);
            page = Math.Max(0, page);
        }

        var category = FindTypedCategory(sectionSlug, categorySlug);
        if (category is null)
        {
            return BindText("Раздел не найден.", callbackAnswerText: "Не найдено");
        }

        var bindings = (await _bindingStore.ListAsync(cancellationToken))
            .Where(binding =>
                binding.IsActive &&
                binding.IsLibraryVisible &&
                string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(binding.Series, SectionStorageSeries(sectionSlug), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ClassifyTypedBinding(sectionSlug, binding), category.Slug, StringComparison.Ordinal) &&
                CanAccessBinding(access, binding))
            .OrderBy(binding => DisplayBindingTitle(binding), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return BuildFileList(
            $"{sectionTitle} — {category.Label}",
            bindings,
            page,
            pageNumber => $"{callbackPrefix}{category.Slug}:p:{pageNumber}",
            backCallback,
            category.Label);
    }

    private static TelegramManualLibraryResult BuildOutdoorProductLines()
    {
        var rows = SupportedSeries
            .Select(series => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton(series.DisplayName, $"{LibraryGreeProductPrefix}{series.Slug}")])
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryGreeCallback)])
            .ToArray();

        return BindText(
            "Gree / Наружные\n\nВыберите продуктовую линейку:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Наружные");
    }

    private static TelegramManualLibraryResult BuildOutdoorDocumentBuckets(ManualSeriesOption series)
    {
        var rows = OutdoorDocumentTypes()
            .Select(documentType => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton(documentType.Label, $"{LibraryGreeProductPrefix}{series.Slug}:{documentType.Slug}")])
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryGreeOutdoorCallback)])
            .ToArray();

        return BindText(
            $"Gree / Наружные / {series.DisplayName}\n\nВыберите тип документа:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            series.DisplayName);
    }

    private async Task<TelegramManualLibraryResult> BuildOutdoorBucketAsync(
        ManualSeriesOption series,
        ManualDocumentTypeOption documentType,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var bindings = (await _bindingStore.ListAsync(cancellationToken))
            .Where(binding =>
                binding.IsActive &&
                binding.IsLibraryVisible &&
                binding.DocumentType == documentType.DocumentType &&
                string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(binding.Series, series.Series, StringComparison.OrdinalIgnoreCase) &&
                CanAccessBinding(access, binding))
            .OrderBy(binding => DisplayBindingTitle(binding), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (bindings.Length == 0)
        {
            return BindText(
                $"Gree / Наружные / {series.DisplayName} / {documentType.Label}\n\nПока файлов нет.",
                new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
                [
                    [new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", $"{LibraryGreeProductPrefix}{series.Slug}")]
                ]),
                "Пока файлов нет");
        }

        return BuildFileList(
            $"Gree / Наружные / {series.DisplayName} / {documentType.Label}",
            bindings,
            0,
            _ => $"{LibraryGreeProductPrefix}{series.Slug}:{documentType.Slug}",
            $"{LibraryGreeProductPrefix}{series.Slug}",
            documentType.Label);
    }

    private async Task<TelegramManualLibraryResult> BuildFreeLibrarySectionAsync(
        ManualLibrarySectionOption section,
        TelegramUserAccessResult access,
        int page,
        CancellationToken cancellationToken)
    {
        var bindings = (await _bindingStore.ListAsync(cancellationToken))
            .Where(binding =>
                binding.IsActive &&
                binding.IsLibraryVisible &&
                string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(binding.Series, section.StorageSeries, StringComparison.OrdinalIgnoreCase) &&
                IsVisibleLibraryDocumentType(binding.DocumentType) &&
                IsAllowedLibraryBinding(binding) &&
                CanAccessBinding(access, binding))
            .OrderBy(binding => DisplayBindingTitle(binding), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var totalPages = Math.Max(1, (int)Math.Ceiling(bindings.Length / (double)FreeSectionPageSize));
        page = Math.Clamp(page, 0, totalPages - 1);
        var pageItems = bindings
            .Skip(page * FreeSectionPageSize)
            .Take(FreeSectionPageSize)
            .ToArray();

        var rows = pageItems
            .Select((binding, index) => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton(LibraryFileButtonLabel(binding, page * FreeSectionPageSize + index + 1), LibraryFileCallbackData(binding))])
            .ToList();
        if (totalPages > 1)
        {
            var navigation = new List<EquipmentDiagnosticTelegramInlineKeyboardButton>();
            if (page > 0)
            {
                navigation.Add(new EquipmentDiagnosticTelegramInlineKeyboardButton("‹", FreeSectionCallback(section.Slug, page - 1)));
            }

            if (page + 1 < totalPages)
            {
                navigation.Add(new EquipmentDiagnosticTelegramInlineKeyboardButton("›", FreeSectionCallback(section.Slug, page + 1)));
            }

            if (navigation.Count > 0)
            {
                rows.Add(navigation);
            }
        }

        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryGreeCallback)]);
        var text = bindings.Length == 0
            ? $"Gree / {section.DisplayName}\n\nПока файлов нет."
            : $"Gree / {section.DisplayName}\n\nДоступные файлы:";
        return BindText(
            text,
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            bindings.Length == 0 ? "Пока файлов нет" : section.DisplayName);
    }

    private static TelegramManualLibraryResult BuildFileList(
        string heading,
        IReadOnlyList<TelegramManualFileBinding> bindings,
        int page,
        Func<int, string> pageCallback,
        string backCallback,
        string callbackAnswerText)
    {
        var totalPages = Math.Max(1, (int)Math.Ceiling(bindings.Count / (double)FreeSectionPageSize));
        page = Math.Clamp(page, 0, totalPages - 1);
        var pageItems = bindings
            .Skip(page * FreeSectionPageSize)
            .Take(FreeSectionPageSize)
            .ToArray();

        var rows = pageItems
            .Select((binding, index) => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton(LibraryFileButtonLabel(binding, page * FreeSectionPageSize + index + 1), LibraryFileCallbackData(binding))])
            .ToList();

        if (totalPages > 1)
        {
            var navigation = new List<EquipmentDiagnosticTelegramInlineKeyboardButton>();
            if (page > 0)
            {
                navigation.Add(new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", pageCallback(page - 1)));
            }

            if (page + 1 < totalPages)
            {
                navigation.Add(new EquipmentDiagnosticTelegramInlineKeyboardButton("Далее", pageCallback(page + 1)));
            }

            if (navigation.Count > 0)
            {
                rows.Add(navigation);
            }
        }

        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", backCallback)]);

        var text = new StringBuilder(heading);
        if (bindings.Count == 0)
        {
            text.AppendLine();
            text.AppendLine();
            text.Append("Пока файлов нет.");
        }
        else
        {
            text.AppendLine();
            text.AppendLine();
            for (var i = 0; i < bindings.Count; i++)
            {
                text.Append(i + 1);
                text.Append(". ");
                text.AppendLine(SafeFileName(bindings[i].OriginalFileName) ?? DisplayBindingTitle(bindings[i]));
            }
        }

        return BindText(
            text.ToString().Trim(),
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            bindings.Count == 0 ? "Пока файлов нет" : callbackAnswerText);
    }

    private static TelegramManualLibraryResult BuildSectionDocumentBuckets(ManualLibrarySectionOption section)
    {
        var rows = DocumentTypeOptions
            .Where(documentType => section.DocumentTypes.Contains(documentType.DocumentType))
            .Where(documentType => IsVisibleLibraryDocumentType(documentType.DocumentType))
            .Select(documentType => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton(documentType.Label, $"{FreeSectionCallback(section.Slug)}:{documentType.Slug}")])
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryGreeCallback)])
            .ToArray();

        return BindText(
            $"Gree / {section.DisplayName}\n\nВыберите тип документа:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            section.DisplayName);
    }

    private async Task<TelegramManualLibraryResult> BuildSectionBucketAsync(
        ManualLibrarySectionOption section,
        ManualDocumentTypeOption documentType,
        TelegramUserAccessResult access,
        int page,
        CancellationToken cancellationToken)
    {
        var bindings = (await _bindingStore.ListAsync(cancellationToken))
            .Where(binding =>
                binding.IsActive &&
                binding.IsLibraryVisible &&
                binding.DocumentType == documentType.DocumentType &&
                string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(binding.Series, section.StorageSeries, StringComparison.OrdinalIgnoreCase) &&
                CanAccessBinding(access, binding))
            .OrderBy(binding => DisplayBindingTitle(binding), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return BuildFileList(
            $"Gree / {section.DisplayName} / {documentType.Label}",
            bindings,
            page,
            pageNumber => $"{FreeSectionCallback(section.Slug)}:{documentType.Slug}:p:{pageNumber}",
            FreeSectionCallback(section.Slug),
            documentType.Label);
    }

    private async Task<TelegramManualLibraryResult> BuildAccessRequestsAsync(
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (!CanManageLibrary(access.Role))
        {
            return BindText("Управление доступом к библиотеке доступно только владельцу.", callbackAnswerText: "Нет доступа");
        }

        var requests = await _libraryAccessStore.ListPendingRequestsAsync(10, cancellationToken);
        if (requests.Count == 0)
        {
            return BindText(
                "Новых запросов доступа нет.",
                new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
                [
                    [new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)]
                ]),
                "Запросов нет");
        }

        var rows = requests
            .SelectMany(request => new[]
            {
                (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                    [new EquipmentDiagnosticTelegramInlineKeyboardButton($"Одобрить #{request.Id}", LibraryApprovePrefix + request.Id)],
                [new EquipmentDiagnosticTelegramInlineKeyboardButton($"Отклонить #{request.Id}", LibraryRejectPrefix + request.Id)]
            })
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)])
            .ToArray();
        var text = new StringBuilder("Запросы доступа");
        foreach (var request in requests)
        {
            text.AppendLine();
            var user = await _userStore.GetByIdAsync(request.TelegramUserId, cancellationToken);
            text.AppendLine($"#{request.Id}: {AccessRequestUserLine(user, request)}");
            text.AppendLine($"chat: {request.TelegramChatId}");
        }

        return BindText(text.ToString().Trim(), new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows));
    }

    private async Task<TelegramManualLibraryResult> BuildAccessManagementAsync(
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (!CanManageLibrary(access.Role))
        {
            return BindText("Управление доступом к библиотеке доступно только владельцу.", callbackAnswerText: "Нет доступа");
        }

        var users = (await _userStore.ListUsersAsync(20, cancellationToken))
            .Where(user => user.Role is TelegramUserRole.Admin or TelegramUserRole.Engineer or TelegramUserRole.Installer)
            .OrderBy(user => UserDisplayName(user), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (users.Length == 0)
        {
            return BindText(
                "Управление доступом\n\nПока нет пользователей, которым можно выдать доступ.",
                new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
                [
                    [new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)]
                ]),
                "Нет пользователей");
        }

        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>();
        var text = new StringBuilder("Управление доступом");
        foreach (var user in users)
        {
            var hasGrant = await _libraryAccessStore.HasActiveGrantAsync(user.Id, cancellationToken);
            text.AppendLine();
            text.AppendLine($"- {UserIdentityLine(user)}");
            text.AppendLine($"  chat: {user.TelegramChatId}; access: {(hasGrant ? "active" : "none")}");
            rows.Add(
            [
                new EquipmentDiagnosticTelegramInlineKeyboardButton(
                    hasGrant ? $"Отозвать {ShortUserLabel(user)}" : $"Выдать {ShortUserLabel(user)}",
                    hasGrant ? LibraryRevokePrefix + user.Id : LibraryGrantPrefix + user.Id)
            ]);
        }

        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)]);
        return BindText(text.ToString().Trim(), new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows));
    }

    private async Task<TelegramManualLibraryResult> SendLibraryFileAsync(
        string fileKey,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var binding = await _bindingStore.GetAsync(fileKey, cancellationToken);
        if (binding is null)
        {
            var series = SupportedSeries.FirstOrDefault(item =>
                SeriesSlug(item.Series) == fileKey ||
                item.Slug.Equals(fileKey, StringComparison.Ordinal));
            binding = series is null
                ? null
                : (await _bindingStore.ListAsync(cancellationToken))
                    .Where(item =>
                        item.IsActive &&
                        string.Equals(item.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(item.Series, series.Series, StringComparison.OrdinalIgnoreCase) &&
                        item.DocumentType == TelegramLibraryDocumentType.ServiceManual)
                    .OrderByDescending(item => item.RegisteredAtUtc)
                    .FirstOrDefault();
        }

        if (binding is null || !CanSendLibraryBinding(binding, access))
        {
            return BindText("Файл недоступен или был заменён.", callbackAnswerText: "Нет доступа");
        }

        return BuildLibraryFileDelivery(binding, access);
    }

    private async Task<TelegramManualLibraryResult> SendLibraryFileByIdAsync(
        string idText,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(idText, out var id) || id <= 0)
        {
            return BindText("Файл недоступен или был заменён.", callbackAnswerText: "Файл недоступен");
        }

        var binding = await _bindingStore.GetByIdAsync(id, cancellationToken);
        if (binding is null || !CanSendLibraryBinding(binding, access))
        {
            return BindText("Файл недоступен или был заменён.", callbackAnswerText: "Нет доступа");
        }

        return BuildLibraryFileDelivery(binding, access);
    }

    private static TelegramManualLibraryResult BuildLibraryFileDelivery(
        TelegramManualFileBinding binding,
        TelegramUserAccessResult access)
    {
        var replyMarkup = TelegramDiagnosticConversationService.MainKeyboard(access);
        var text = $"Отправляю файл: {DisplayBindingTitle(binding)}";
        return new TelegramManualLibraryResult(
            text,
            [],
            [
                new EquipmentDiagnosticTelegramOutboundMessage(text),
                new EquipmentDiagnosticTelegramOutboundMessage(
                    DisplayBindingTitle(binding),
                    ReplyMarkup: replyMarkup,
                    DocumentFileId: binding.TelegramFileId,
                    DocumentFileName: binding.OriginalFileName,
                    ProtectContent: true)
            ],
            CallbackAnswerText: "Отправляю файл",
            ReplyMarkup: replyMarkup);
    }

    private async Task<TelegramManualLibraryResult> SendDiagnosticOwnerManualFileAsync(
        string fileToken,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (!_options.ManualLibrary.Enabled)
        {
            return ManualUnavailable("Библиотека руководств сейчас выключена.");
        }

        if (!IsActiveUser(access) || access.User is null)
        {
            return ManualUnavailable("Доступ ограничен.");
        }

        var last = await _historyStore.GetLastForTelegramUserAsync(access.User.Id, cancellationToken);
        var series = last is null ? null : DiagnosticSeries(last);
        if (last is null ||
            last.Status != TelegramDiagnosticCaseStatus.Completed ||
            string.IsNullOrWhiteSpace(last.Manufacturer) ||
            !string.Equals(last.Manufacturer, BrandGree, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(last.Code) ||
            string.IsNullOrWhiteSpace(series))
        {
            return MissingDiagnosticContext();
        }

        var bindings = await ListDiagnosticOwnerManualsAsync(series, cancellationToken);
        var binding = ResolveDiagnosticOwnerManualSelection(fileToken, bindings);
        if (binding is null)
        {
            return Html(
                $"{TelegramHtml.Bold("Руководство недоступно")}\n\n" +
                "Выбранный файл больше не доступен для диагностики.",
                callbackAnswerText: "Файл недоступен",
                replyMarkup: TelegramDiagnosticConversationService.DiagnosticManualContextKeyboard(access));
        }

        var equipment = $"{last.Manufacturer} {series}";
        var heading =
            $"{TelegramHtml.Bold("Руководство по диагностике")}\n\n" +
            $"{TelegramHtml.Bold("Оборудование:")} {TelegramHtml.Escape(equipment)}\n" +
            $"{TelegramHtml.Bold("Код:")} {TelegramHtml.Escape(last.Code)}\n" +
            $"{TelegramHtml.Bold("Файл:")} {TelegramHtml.Escape(DisplayBindingTitle(binding))}\n\n" +
            "Отправляю выбранное Owner/User руководство для этой серии.";
        var replyMarkup = TelegramDiagnosticConversationService.DiagnosticManualContextKeyboard(access);
        return new TelegramManualLibraryResult(
            heading,
            [],
            [
                new EquipmentDiagnosticTelegramOutboundMessage(
                    heading,
                    ParseMode: TelegramHtml.ParseMode,
                    ReplyMarkup: replyMarkup),
                new EquipmentDiagnosticTelegramOutboundMessage(
                    DisplayBindingTitle(binding),
                    ReplyMarkup: replyMarkup,
                    DocumentFileId: binding.TelegramFileId,
                    DocumentFileName: binding.OriginalFileName,
                    ProtectContent: true)
            ],
            TelegramHtml.ParseMode,
            "Отправляю руководство",
            replyMarkup);
    }

    private async Task<TelegramManualFileBinding[]> ListDiagnosticOwnerManualsAsync(
        string series,
        CancellationToken cancellationToken) =>
        (await _bindingStore.ListAsync(cancellationToken))
        .Where(binding =>
            binding.IsActive &&
            binding.CanUseForDiagnostics &&
            binding.DocumentType == TelegramLibraryDocumentType.OwnerManual &&
            string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(binding.Series, series, StringComparison.OrdinalIgnoreCase))
        .OrderBy(binding => DiagnosticOwnerManualSortNumber(binding) ?? int.MaxValue)
        .ThenBy(binding => DisplayBindingTitle(binding), StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static TelegramManualLibraryResult BuildDiagnosticOwnerManualSelection(
        TelegramDiagnosticCaseSnapshot last,
        string series,
        IReadOnlyList<TelegramManualFileBinding> bindings)
    {
        var equipment = $"{last.Manufacturer} {series}";
        var rows = bindings
            .Select((binding, index) => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [
                    new EquipmentDiagnosticTelegramInlineKeyboardButton(
                        DiagnosticOwnerManualButtonLabel(binding, index + 1),
                        DiagnosticOwnerManualCallbackData(binding))
                ])
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", DiagnosticManualCallbackData)])
            .ToArray();
        var fileList = string.Join(
            "\n",
            bindings.Select((binding, index) =>
                $"{index + 1}. {TelegramHtml.Escape(DisplayBindingTitle(binding))}"));
        var text =
            $"{TelegramHtml.Bold("Выберите руководство")}\n\n" +
            $"{TelegramHtml.Bold("Оборудование:")} {TelegramHtml.Escape(equipment)}\n" +
            $"{TelegramHtml.Bold("Код:")} {TelegramHtml.Escape(last.Code)}\n\n" +
            "Для этой серии доступно несколько Owner/User руководств.";

        text = $"{text}\n\n{fileList}";

        return Html(
            text,
            callbackAnswerText: "Выберите файл",
            replyMarkup: new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows));
    }

    private static TelegramManualFileBinding? ResolveDiagnosticOwnerManualSelection(
        string fileToken,
        IReadOnlyList<TelegramManualFileBinding> bindings) =>
        bindings.FirstOrDefault(binding =>
            string.Equals(DiagnosticOwnerManualToken(binding), fileToken, StringComparison.Ordinal)) ??
        bindings.FirstOrDefault(binding =>
            string.Equals(binding.ManualId, fileToken, StringComparison.OrdinalIgnoreCase));

    private static string DiagnosticOwnerManualCallbackData(TelegramManualFileBinding binding)
    {
        var callbackData = DiagnosticManualFilePrefix + DiagnosticOwnerManualToken(binding);
        return Encoding.UTF8.GetByteCount(callbackData) <= TelegramCallbackDataMaxBytes
            ? callbackData
            : throw new InvalidOperationException("Diagnostic manual callback_data exceeds Telegram limit.");
    }

    private static string DiagnosticOwnerManualToken(TelegramManualFileBinding binding) =>
        Base36(Fnv1A64(binding.ManualId));

    private static string DiagnosticOwnerManualButtonLabel(
        TelegramManualFileBinding binding,
        int number) =>
        TrimUtf8($"{number}) {ShortOwnerManualTitle(DisplayBindingTitle(binding))}", TelegramInlineButtonTextMaxBytes);

    private static int? DiagnosticOwnerManualSortNumber(TelegramManualFileBinding binding)
    {
        var title = ShortOwnerManualTitle(DisplayBindingTitle(binding));
        var start = -1;
        for (var i = 0; i < title.Length; i++)
        {
            if (!char.IsDigit(title[i]))
            {
                continue;
            }

            start = i;
            break;
        }

        if (start < 0)
        {
            return null;
        }

        var end = start;
        while (end < title.Length && char.IsDigit(title[end]))
        {
            end++;
        }

        return int.TryParse(title[start..end], out var value)
            ? value
            : null;
    }

    private static string ShortOwnerManualTitle(string title)
    {
        var name = Path.GetFileNameWithoutExtension(title).Trim();
        foreach (var marker in new[]
        {
            "Owner Manual EN ",
            "Owner/User Manual EN ",
            "User Manual EN ",
            "Owner Manual ",
            "User Manual "
        })
        {
            var index = name.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return name[(index + marker.Length)..].Trim();
            }
        }

        return string.IsNullOrWhiteSpace(name)
            ? title.Trim()
            : name;
    }

    private static string TrimUtf8(string value, int maxBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maxBytes)
        {
            return value;
        }

        const string suffix = "...";
        var suffixBytes = Encoding.UTF8.GetByteCount(suffix);
        var builder = new StringBuilder();
        var byteCount = 0;
        foreach (var ch in value)
        {
            var chBytes = Encoding.UTF8.GetByteCount(ch.ToString());
            if (byteCount + chBytes + suffixBytes > maxBytes)
            {
                break;
            }

            builder.Append(ch);
            byteCount += chBytes;
        }

        return builder.Append(suffix).ToString();
    }

    private static ulong Fnv1A64(string value)
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offsetBasis;
        foreach (var b in Encoding.UTF8.GetBytes(value.Trim().ToUpperInvariant()))
        {
            hash ^= b;
            hash *= prime;
        }

        return hash;
    }

    private static string Base36(ulong value)
    {
        const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
        Span<char> buffer = stackalloc char[13];
        var position = buffer.Length;
        do
        {
            buffer[--position] = alphabet[(int)(value % 36)];
            value /= 36;
        }
        while (value > 0);

        return new string(buffer[position..]);
    }

    private async Task<TelegramManualFileBinding?> FindExistingManualBindingAsync(
        ManualBindSession session,
        CancellationToken cancellationToken)
    {
        if (session.Section is null || session.DocumentType is null || session.Candidate is null)
        {
            return null;
        }

        if (UsesManualIdStorage(session))
        {
            return await _bindingStore.GetAsync(ManualBindingId(session, session.Candidate), cancellationToken);
        }

        return (await _bindingStore.ListAsync(cancellationToken))
            .Where(binding =>
                binding.IsActive &&
                binding.DocumentType == session.DocumentType.DocumentType &&
                string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(binding.Series, session.Series?.Series, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(binding => binding.RegisteredAtUtc)
            .FirstOrDefault();
    }

    private async Task<bool> NotifyAccessRequestResolvedAsync(
        TelegramUserSnapshot user,
        bool approved,
        CancellationToken cancellationToken)
    {
        var text = approved
            ? "Доступ к библиотеке файлов выдан. Теперь раздел 📚 Библиотека доступен в меню."
            : "Запрос доступа к библиотеке отклонён.";

        return await TrySendLibraryAccessNotificationAsync(user, text, cancellationToken);
    }

    private Task<bool> NotifyLibraryAccessChangedAsync(
        TelegramUserSnapshot user,
        bool granted,
        CancellationToken cancellationToken)
    {
        var text = granted
            ? "Доступ к библиотеке файлов выдан. Теперь раздел 📚 Библиотека доступен в меню."
            : "Доступ к библиотеке файлов отозван.";
        return TrySendLibraryAccessNotificationAsync(user, text, cancellationToken);
    }

    private async Task<bool> TrySendLibraryAccessNotificationAsync(
        TelegramUserSnapshot user,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            var notificationAccess = await BuildNotificationAccessAsync(user, cancellationToken);
            var result = await _outboundClient.SendMessageAsync(
                user.TelegramChatId,
                text,
                parseMode: null,
                disableWebPagePreview: true,
                replyMarkup: TelegramDiagnosticConversationService.MainKeyboard(notificationAccess),
                cancellationToken);
            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Telegram library access user notification failed; committed access state remains unchanged. UserDatabaseId: {UserDatabaseId}.",
                    user.Id);
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
                "Telegram library access user notification failed; committed access state remains unchanged. UserDatabaseId: {UserDatabaseId}; ExceptionType: {ExceptionType}.",
                user.Id,
                exception.GetType().Name);
            return false;
        }
    }

    private async Task<TelegramUserAccessResult> BuildNotificationAccessAsync(
        TelegramUserSnapshot user,
        CancellationToken cancellationToken)
    {
        var hasGrant = user.Role is TelegramUserRole.Admin or TelegramUserRole.Engineer or TelegramUserRole.Installer &&
            await _libraryAccessStore.HasActiveGrantAsync(user.Id, cancellationToken);
        return new TelegramUserAccessResult(
            IsAllowed: user.IsEnabled && !user.IsBlocked,
            User: user,
            Role: user.Role,
            HasLibraryAccessGrant: hasGrant);
    }

    private static string NotificationStatusText(bool sent) =>
        sent ? string.Empty : "\n\nУведомление пользователю не отправлено. Основное действие сохранено.";

    private static bool IsActiveUser(TelegramUserAccessResult access) =>
        access.IsAllowed &&
        access.User is { IsEnabled: true, IsBlocked: false };

    private static bool CanManageLibrary(TelegramUserRole role) =>
        TelegramUserRolePolicy.CanManageTelegramLibrary(role);

    private static bool CanManageManuals(TelegramUserRole role) =>
        role == TelegramUserRole.Owner;

    private static bool CanGrantLibraryTo(TelegramUserSnapshot user) =>
        user.IsEnabled &&
        !user.IsBlocked &&
        user.Role is TelegramUserRole.Admin or TelegramUserRole.Engineer or TelegramUserRole.Installer;

    private static bool CanAccessBinding(
        TelegramUserAccessResult access,
        TelegramManualFileBinding binding) =>
        access.User is { IsEnabled: true, IsBlocked: false } &&
        access.CanAccessLibrary &&
        TelegramUserRolePolicy.HasAtLeastRole(access.Role, binding.MinRole);

    private static bool CanSendLibraryBinding(
        TelegramManualFileBinding? binding,
        TelegramUserAccessResult access) =>
        binding is not null &&
        binding.IsActive &&
        binding.IsLibraryVisible &&
        CanAccessBinding(access, binding) &&
        IsAllowedLibraryBinding(binding);

    private static bool IsAllowedLibraryBinding(TelegramManualFileBinding binding)
    {
        if (!string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(binding.Series, "Indoor", StringComparison.OrdinalIgnoreCase))
        {
            return ClassifyIndoorBinding(binding) is not null;
        }

        if (string.Equals(binding.Series, "Controllers", StringComparison.OrdinalIgnoreCase))
        {
            return ClassifyControllerBinding(binding) is not null;
        }

        if (string.Equals(binding.Series, "Accessories", StringComparison.OrdinalIgnoreCase))
        {
            return binding.DocumentType is TelegramLibraryDocumentType.OwnerManual or
                TelegramLibraryDocumentType.ControllerGuide;
        }

        if (string.Equals(binding.Series, "U-Match R32", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(binding.Series, "ERV B Series", StringComparison.OrdinalIgnoreCase))
        {
            return binding.DocumentType is TelegramLibraryDocumentType.ServiceManual or
                TelegramLibraryDocumentType.OwnerManual;
        }

        return SupportedSeries.Any(series =>
            string.Equals(series.Series, binding.Series, StringComparison.OrdinalIgnoreCase) &&
            binding.DocumentType is TelegramLibraryDocumentType.ServiceManual or
                TelegramLibraryDocumentType.OwnerManual);
    }

    private static string? ClassifyTypedBinding(
        string sectionSlug,
        TelegramManualFileBinding binding) =>
        sectionSlug switch
        {
            IndoorSectionSlug => ClassifyIndoorBinding(binding),
            ControllersSectionSlug => ClassifyControllerBinding(binding),
            _ => null
        };

    private static string? ClassifyIndoorBinding(TelegramManualFileBinding binding)
    {
        var text = BindingSearchText(binding);
        if (binding.DocumentType == TelegramLibraryDocumentType.ServiceManual ||
            ContainsAny(text, "Service Manual"))
        {
            return "svc";
        }

        if (!IsOwnerLikeDocument(binding.DocumentType))
        {
            return null;
        }

        if (ContainsAny(text, "Wall Mounted", "Wall"))
        {
            return "wall";
        }

        if (ContainsAny(text, "One-way Cassette", "Cassette", "TD_A", "TD-A", "TD B", "TD_B", "TD-B", "T_C", "T-C", "T_D1", "T-D1"))
        {
            return "cas";
        }

        if (ContainsAny(text, "Ducted", "Duct", "Static", "Low Static", "Middle Static", "High Static", "Super High Static", "PL", "PLS", "PMS", "PHS"))
        {
            return "duc";
        }

        return null;
    }

    private static string? ClassifyControllerBinding(TelegramManualFileBinding binding)
    {
        var text = BindingSearchText(binding);
        if (ContainsAny(text, "Remote Controller", "Wireless", " YAP", " YV"))
        {
            return "ir";
        }

        if (binding.DocumentType == TelegramLibraryDocumentType.ControllerGuide &&
            ContainsAny(text, "Wired Controller", "XK", "XE7A"))
        {
            return "wall";
        }

        return null;
    }

    private static bool IsOwnerLikeDocument(TelegramLibraryDocumentType documentType) =>
        documentType is TelegramLibraryDocumentType.OwnerManual or TelegramLibraryDocumentType.UserGuide;

    private static bool ContainsAny(string value, params string[] tokens) =>
        tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static string BindingSearchText(TelegramManualFileBinding binding) =>
        string.Join(
            ' ',
            new[] { binding.OriginalFileName, binding.Title }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .SelectMany(value => new[] { value!, value!.Replace('_', ' ') }));

    private static string SectionStorageSeries(string sectionSlug) =>
        FindSection(sectionSlug)?.StorageSeries ?? string.Empty;

    private static TypedLibraryCategoryOption? FindTypedCategory(
        string sectionSlug,
        string categorySlug) =>
        TypedCategories(sectionSlug)
            .FirstOrDefault(category => category.Slug.Equals(categorySlug, StringComparison.Ordinal));

    private static IReadOnlyList<TypedLibraryCategoryOption> TypedCategories(string sectionSlug) =>
        sectionSlug switch
        {
            IndoorSectionSlug => IndoorCategories,
            ControllersSectionSlug => ControllerCategories,
            _ => Array.Empty<TypedLibraryCategoryOption>()
        };

    private static string AccessRequestUserLine(
        TelegramUserSnapshot? user,
        TelegramLibraryAccessRequest request) =>
        user is null
            ? $"Пользователь без имени — {request.RequestedRole}"
            : UserIdentityLine(user);

    private static string UserIdentityLine(TelegramUserSnapshot user)
    {
        var displayName = UserDisplayName(user);
        var username = string.IsNullOrWhiteSpace(user.Username)
            ? null
            : $" (@{user.Username.Trim().TrimStart('@')})";
        return $"{displayName}{username} — {user.Role}";
    }

    private static string UserDisplayName(TelegramUserSnapshot user)
    {
        var parts = new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();
        if (parts.Length > 0)
        {
            return string.Join(' ', parts);
        }

        return string.IsNullOrWhiteSpace(user.Username)
            ? "Пользователь без имени"
            : $"@{user.Username.Trim().TrimStart('@')}";
    }

    private static string ShortUserLabel(TelegramUserSnapshot user)
    {
        var label = UserDisplayName(user);
        return label.Length <= 24 ? label : label[..21] + "...";
    }

    private static string SafeUserLabel(TelegramUserSnapshot user) =>
        !string.IsNullOrWhiteSpace(user.Username)
            ? $"@{user.Username}"
            : $"chat {user.TelegramChatId}";

    private static string DisplayBindingTitle(TelegramManualFileBinding binding) =>
        string.IsNullOrWhiteSpace(binding.Title)
            ? SafeFileName(binding.OriginalFileName) ?? "Telegram file"
            : binding.Title.Trim();

    private static string LibraryFileCallbackData(TelegramManualFileBinding binding)
    {
        if (binding.Id is not > 0)
        {
            throw new InvalidOperationException("Library file callbacks require a persisted binding id.");
        }

        var callbackData = LibraryFileByIdPrefix + binding.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Encoding.UTF8.GetByteCount(callbackData) <= TelegramCallbackDataMaxBytes
            ? callbackData
            : throw new InvalidOperationException("Library file callback_data exceeds Telegram limit.");
    }

    private static string LibraryFileButtonLabel(
        TelegramManualFileBinding binding,
        int number) =>
        TrimUtf8($"{number}) {ShortLibraryFileTitle(binding)}", TelegramInlineButtonTextMaxBytes);

    private static string ShortLibraryFileTitle(TelegramManualFileBinding binding)
    {
        var title = Path.GetFileNameWithoutExtension(DisplayBindingTitle(binding)).Trim();
        if (title.Length == 0)
        {
            return "Файл";
        }

        var replacements = new[]
        {
            "Gree GMV ",
            "Gree_GMV_",
            "Gree ",
            "Indoor Unit ",
            "Indoor Units ",
            "Owner Manual EN ",
            "Service Manual EN ",
            "Wired Controller ",
            "Remote Controller "
        };

        foreach (var replacement in replacements)
        {
            title = title.Replace(replacement, string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        title = title
            .Replace('_', ' ')
            .Replace("One way", "One-way", StringComparison.OrdinalIgnoreCase)
            .Replace("XE7A 23H XE7A 23HC", "XE7A-23H/23HC", StringComparison.OrdinalIgnoreCase)
            .Replace("XE7A 24H XE7A 24HC", "XE7A-24H/24HC", StringComparison.OrdinalIgnoreCase)
            .Replace("YAP1F YV1L1", "YAP1F/YV1L1", StringComparison.OrdinalIgnoreCase)
            .Replace("GC202603 I", "GC202603-I", StringComparison.OrdinalIgnoreCase)
            .Trim();

        while (title.Contains("  ", StringComparison.Ordinal))
        {
            title = title.Replace("  ", " ", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(title) ? "Файл" : title;
    }

    private static string SeriesSlug(string? value) =>
        NormalizeFileToken(value ?? string.Empty);

    private static string FreeSectionCallback(string sectionSlug, int page = 0) =>
        page <= 0
            ? LibraryGreeSectionPrefix + sectionSlug
            : $"{LibraryGreeSectionPrefix}{sectionSlug}:p:{page}";

    private static string RecommendedFileName(ManualBindSession session) =>
        session.Section is null || session.DocumentType is null
            ? "Gree manual.pdf"
            : RecommendedFileName(session.Section, session.Series, session.DocumentType);

    private static string RecommendedFileName(
        ManualLibrarySectionOption section,
        ManualSeriesOption? series,
        ManualDocumentTypeOption documentType) =>
        section.IsOutdoor && series is not null
            ? series.RecommendedFileName.Replace("Service Manual", PlainDocumentTypeLabel(documentType.DocumentType), StringComparison.OrdinalIgnoreCase)
            : $"Gree {section.DisplayName} {PlainDocumentTypeLabel(documentType.DocumentType)}.pdf";

    private static string BuildManualBindTargetLabel(ManualBindSession session) =>
        session.Section is null || session.DocumentType is null
            ? "Gree"
            : BuildManualBindTargetLabel(session.Section, session.Series, session.DocumentType);

    private static string BuildManualBindTargetLabel(
        ManualLibrarySectionOption section,
        ManualSeriesOption? series,
        ManualDocumentTypeOption documentType) =>
        section.IsOutdoor && series is not null
            ? $"Gree / Наружные / {series.DisplayName} / {documentType.Label}"
            : $"Gree / {section.DisplayName} / {documentType.Label}";

    private static string PlainDocumentTypeLabel(TelegramLibraryDocumentType documentType) =>
        documentType switch
        {
            TelegramLibraryDocumentType.ServiceManual => "Service Manual",
            TelegramLibraryDocumentType.OwnerManual => "Owner Manual",
            TelegramLibraryDocumentType.InstallationManual => "Installation Manual",
            TelegramLibraryDocumentType.ControllerGuide => "Controller Guide",
            _ => documentType.ToString()
        };

    private static EquipmentDiagnosticTelegramReplyMarkup AccessRequestKeyboard() =>
        new(
            InlineKeyboard:
            [
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("🔐 Запросить доступ", LibraryRequestAccessCallback)],
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", LibraryCancelCallback)]
            ]);

    private IReadOnlyList<TelegramManualRegistryEntry> ResolveManuals(
        TelegramDiagnosticCaseSnapshot diagnosticCase,
        TelegramUserRole role,
        string? requiredSeries = null)
    {
        var manuals = _manualRegistry.GetManuals();
        var entries = _localizedKnowledge.GetEntries()
            .Where(entry =>
                string.Equals(entry.Code, diagnosticCase.Code, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(diagnosticCase.Manufacturer) ||
                 string.Equals(entry.Manufacturer, diagnosticCase.Manufacturer, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (!string.IsNullOrWhiteSpace(requiredSeries))
        {
            entries = entries
                .Where(entry => string.Equals(entry.Series, requiredSeries, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

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
        role == TelegramUserRole.Owner;

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
                update.DocumentFileSize,
                update.DocumentFileUniqueId);
        }

        return string.IsNullOrWhiteSpace(update.ReplyToDocumentFileId)
            ? null
            : new RegistrationDocument(
                update.ReplyToDocumentFileId,
                update.ReplyToDocumentFileName,
                update.ReplyToDocumentMimeType,
                update.ReplyToDocumentFileSize,
                update.ReplyToDocumentFileUniqueId);
    }

    private static ManualSeriesOption? FindSeries(string slug) =>
        SupportedSeries.FirstOrDefault(item => item.Slug.Equals(slug, StringComparison.Ordinal));

    private static ManualLibrarySectionOption? FindSection(string slug) =>
        LibrarySections.FirstOrDefault(item => item.Slug.Equals(slug, StringComparison.Ordinal));

    private static ManualDocumentTypeOption? FindDocumentType(string slug) =>
        DocumentTypeOptions.FirstOrDefault(item => item.Slug.Equals(slug, StringComparison.Ordinal));

    private static IEnumerable<ManualDocumentTypeOption> OutdoorDocumentTypes() =>
        DocumentTypeOptions.Where(item =>
            item.DocumentType is TelegramLibraryDocumentType.ServiceManual or
                TelegramLibraryDocumentType.OwnerManual);

    private static bool IsVisibleLibraryDocumentType(TelegramLibraryDocumentType documentType) =>
        VisibleLibraryDocumentTypes.Contains(documentType);

    private static EquipmentDiagnosticTelegramReplyMarkup BrandKeyboard() =>
        new(
            InlineKeyboard:
            [
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Gree", ManualBindBrandPrefix + "gree")],
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", ManualBindCancel)]
            ]);

    private static EquipmentDiagnosticTelegramReplyMarkup SectionKeyboard() =>
        new(
            InlineKeyboard:
            [
                .. LibrarySections.Select(section => new[]
                {
                    new EquipmentDiagnosticTelegramInlineKeyboardButton(section.DisplayName, ManualBindSectionPrefix + section.Slug)
                }),
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", ManualBindCancel)]
            ]);

    private static EquipmentDiagnosticTelegramReplyMarkup SeriesKeyboard() =>
        new(
            InlineKeyboard:
            [
                .. SupportedSeries.Select(series => new[]
                {
                    new EquipmentDiagnosticTelegramInlineKeyboardButton(series.DisplayName, ManualBindSeriesPrefix + series.Slug)
                }),
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", ManualBindCancel)]
            ]);

    private static EquipmentDiagnosticTelegramReplyMarkup DocumentTypeKeyboard(
        IEnumerable<TelegramLibraryDocumentType> documentTypes)
    {
        var allowed = documentTypes.ToHashSet();
        return new EquipmentDiagnosticTelegramReplyMarkup(
            InlineKeyboard:
            [
                .. DocumentTypeOptions
                    .Where(item => allowed.Contains(item.DocumentType))
                    .Select(documentType => new[]
                    {
                        new EquipmentDiagnosticTelegramInlineKeyboardButton(documentType.Label, ManualBindDocumentTypePrefix + documentType.Slug)
                    }),
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", ManualBindCancel)]
            ]);
    }

    private static EquipmentDiagnosticTelegramReplyMarkup CancelKeyboard() =>
        new(
            InlineKeyboard:
            [
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", ManualBindCancel)]
            ]);

    private static EquipmentDiagnosticTelegramReplyMarkup ConfirmKeyboard(bool replace) =>
        new(
            InlineKeyboard:
            [
                [
                    new EquipmentDiagnosticTelegramInlineKeyboardButton(
                        replace ? "Заменить" : "Привязать",
                        replace ? ManualBindReplace : ManualBindConfirm)
                ],
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", ManualBindCancel)]
            ]);

    private static string BuildManualBindDocumentPrompt(
        ManualLibrarySectionOption section,
        ManualSeriesOption? series,
        ManualDocumentTypeOption documentType) =>
        $"Пришлите PDF-файл для {BuildManualBindTargetLabel(section, series, documentType)}.\n\nРекомендуемое имя: {RecommendedFileName(section, series, documentType)}";

    private static bool IsValidManualBindDocument(
        RegistrationDocument document,
        out string reason)
    {
        if (string.IsNullOrWhiteSpace(document.FileId))
        {
            reason = "Telegram file_id не найден. Пришлите файл как документ Telegram.";
            return false;
        }

        var fileName = SafeFileName(document.FileName);
        if (string.IsNullOrWhiteSpace(fileName) ||
            !Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ||
            !IsPdfContentType(document.MimeType))
        {
            reason = "Ожидаю PDF-файл мануала.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsPdfContentType(string? mimeType) =>
        string.IsNullOrWhiteSpace(mimeType) ||
        mimeType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
        mimeType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeFileToken(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder.ToString();
    }

    private static TelegramManualFileBinding CreateManualBinding(
        ManualBindSession session,
        TelegramUserAccessResult access,
        EquipmentDiagnosticTelegramUpdate update)
    {
        if (session.Section is null || session.DocumentType is null || session.Candidate is null)
        {
            throw new InvalidOperationException("Manual bind session is incomplete.");
        }

        var candidate = session.Candidate;
        var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
        return new TelegramManualFileBinding(
            ManualId: ManualBindingId(session, candidate),
            TelegramFileId: candidate.FileId,
            OriginalFileName: candidate.FileName,
            ContentType: candidate.ContentType,
            RegisteredAtUtc: now,
            Source: "TelegramManualBind",
            RegisteredByRole: access.Role.ToString(),
            TelegramFileUniqueId: candidate.FileUniqueId,
            FileSize: candidate.FileSize,
            Brand: BrandGree,
            Series: BindingSeries(session),
            UploadedByTelegramUserId: candidate.UploadedByTelegramUserId,
            UploadedByTelegramChatId: candidate.UploadedByTelegramChatId,
            IsActive: true,
            UpdatedAtUtc: now,
            Title: candidate.FileName,
            DocumentType: session.DocumentType.DocumentType,
            MinRole: MinRoleForDocumentType(session.DocumentType.DocumentType),
            IsLibraryVisible: true,
            CanUseForDiagnostics: IsDiagnosticGuideSeries(session.Section) &&
                session.DocumentType.DocumentType == TelegramLibraryDocumentType.OwnerManual);
    }

    private static bool IsDiagnosticGuideSeries(ManualLibrarySectionOption section) =>
        section.IsOutdoor ||
        section.Slug.Equals(UMatchSectionSlug, StringComparison.Ordinal) ||
        section.Slug.Equals(ErvSectionSlug, StringComparison.Ordinal);

    private static string SeriesManualId(
        ManualSeriesOption series,
        TelegramLibraryDocumentType documentType = TelegramLibraryDocumentType.ServiceManual) =>
        $"gree-{series.Slug}-{DocumentTypeManualIdSlug(documentType)}";

    private static string ManualBindingId(
        ManualBindSession session,
        ManualBindCandidate candidate) =>
        session.Section?.IsOutdoor == true && session.Series is not null && session.DocumentType is not null
            ? session.DocumentType.DocumentType == TelegramLibraryDocumentType.OwnerManual
                ? $"{SeriesManualId(session.Series, session.DocumentType.DocumentType)}-{SafeManualIdToken(candidate.FileName)}"
                : SeriesManualId(session.Series, session.DocumentType.DocumentType)
            : $"gree-{session.Section!.Slug}-{DocumentTypeManualIdSlug(session.DocumentType!.DocumentType)}-{SafeManualIdToken(candidate.FileName)}";

    private static bool UsesManualIdStorage(ManualBindSession session) =>
        session.Section?.IsOutdoor != true ||
        session.DocumentType?.DocumentType == TelegramLibraryDocumentType.OwnerManual;

    private static string BindingSeries(ManualBindSession session) =>
        session.Section?.IsOutdoor == true
            ? session.Series?.Series ?? string.Empty
            : session.Section?.StorageSeries ?? string.Empty;

    private static TelegramUserRole MinRoleForDocumentType(TelegramLibraryDocumentType documentType) =>
        documentType switch
        {
            TelegramLibraryDocumentType.ServiceManual => TelegramUserRole.Engineer,
            TelegramLibraryDocumentType.OwnerManual => TelegramUserRole.Consumer,
            TelegramLibraryDocumentType.InstallationManual => TelegramUserRole.Installer,
            TelegramLibraryDocumentType.ControllerGuide => TelegramUserRole.Installer,
            _ => TelegramUserRole.Engineer
        };

    private static string DocumentTypeManualIdSlug(TelegramLibraryDocumentType documentType) =>
        documentType switch
        {
            TelegramLibraryDocumentType.ServiceManual => "service-manual",
            TelegramLibraryDocumentType.OwnerManual => "owner-manual",
            TelegramLibraryDocumentType.InstallationManual => "installation-manual",
            TelegramLibraryDocumentType.ControllerGuide => "controller-guide",
            _ => NormalizeFileToken(documentType.ToString())
        };

    private static string SafeManualIdToken(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var token = NormalizeFileToken(name);
        return string.IsNullOrWhiteSpace(token)
            ? "file"
            : token.Length <= 64
                ? token
                : token[..64];
    }

    private TelegramManualRegistryEntry? FindManual(string manualId) =>
        _manualRegistry.GetManuals()
            .FirstOrDefault(item => item.ManualId.Equals(manualId, StringComparison.OrdinalIgnoreCase));

    private static string DisplayName(TelegramManualRegistryEntry manual)
    {
        var title = string.IsNullOrWhiteSpace(manual.DisplayNameRu)
            ? manual.DocumentTitle
            : manual.DisplayNameRu;
        return string.IsNullOrWhiteSpace(manual.DocumentCode)
            ? title
            : $"{title} ({manual.DocumentCode})";
    }

    private static string[] SplitCommand(string? text) =>
        text?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

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

    private static string? DiagnosticSeries(TelegramDiagnosticCaseSnapshot diagnosticCase)
    {
        if (string.IsNullOrWhiteSpace(diagnosticCase.NormalizedRequestJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(diagnosticCase.NormalizedRequestJson);
            return document.RootElement.TryGetProperty("series", out var series) &&
                series.ValueKind == JsonValueKind.String
                    ? series.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TelegramManualLibraryResult MissingDiagnosticContext() =>
        Html(
            $"{TelegramHtml.Bold("Мануал недоступен")}\n\n" +
            $"Сначала выполните диагностику конкретного кода, затем нажмите {DiagnosticManualButton}.",
            callbackAnswerText: "Сначала выполните диагностику");

    private static TelegramManualLibraryResult ManualUnavailable(string text) =>
        Html(
            $"{TelegramHtml.Bold("Мануал недоступен")}\n\n{TelegramHtml.Escape(text)}",
            callbackAnswerText: "Мануал недоступен");

    private static TelegramManualLibraryResult Html(
        string text,
        string? callbackAnswerText = null,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null) =>
        new(
            text,
            [],
            ParseMode: TelegramHtml.ParseMode,
            CallbackAnswerText: callbackAnswerText,
            ReplyMarkup: replyMarkup);

    private static TelegramManualLibraryResult Text(string text) => new(text, []);

    private static TelegramManualLibraryResult BindText(
        string text,
        EquipmentDiagnosticTelegramReplyMarkup? replyMarkup = null,
        string? callbackAnswerText = null) =>
        new(text, [], CallbackAnswerText: callbackAnswerText, ReplyMarkup: replyMarkup);

    private readonly record struct RegistrationDocument(
        string FileId,
        string? FileName,
        string? MimeType,
        long? FileSize,
        string? FileUniqueId);

    private sealed record ManualSeriesOption(
        string Slug,
        string DisplayName,
        string Series,
        string RecommendedFileName);

    private sealed record ManualLibrarySectionOption(
        string Slug,
        string DisplayName,
        string StorageSeries,
        bool IsOutdoor,
        IReadOnlyList<TelegramLibraryDocumentType> DocumentTypes);

    private sealed record ManualDocumentTypeOption(
        string Slug,
        TelegramLibraryDocumentType DocumentType,
        string Label);

    private sealed record TypedLibraryCategoryOption(
        string Slug,
        string Label);

    private sealed record ManualBindSession(
        ManualBindStage Stage,
        ManualLibrarySectionOption? Section,
        ManualSeriesOption? Series,
        ManualDocumentTypeOption? DocumentType,
        ManualBindCandidate? Candidate);

    private sealed record ManualBindCandidate(
        string FileId,
        string? FileUniqueId,
        string FileName,
        string? ContentType,
        long? FileSize,
        long? UploadedByTelegramUserId,
        long UploadedByTelegramChatId);

    private enum ManualBindStage
    {
        SelectingBrand,
        SelectingSection,
        SelectingSeries,
        SelectingDocumentType,
        WaitingForDocument,
        WaitingForConfirmation,
        WaitingForReplaceConfirmation
    }
}
