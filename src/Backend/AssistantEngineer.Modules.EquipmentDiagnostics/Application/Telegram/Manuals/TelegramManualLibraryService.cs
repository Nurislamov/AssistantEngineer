using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
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
    public const string LibraryButton = "📚 Библиотека";
    public const string DiagnosticGuideButton = "📘 Руководство";
    private const string LibraryCommand = "/library";
    private const string ManualRequestCommand = "/manuals";
    private const string ManualRegisterCommand = "/manual_register";
    private const string ManualUnregisterCommand = "/manual_unregister";
    private const string ManualBindingsCommand = "/manual_bindings";
    private const string ManualBindCommand = "/manual_bind";
    private const string ManualBindCallbackPrefix = "mb:";
    private const string ManualBindSeriesPrefix = "mb:s:";
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
    private const string LibraryRequestsCallback = "lib:reqs";
    private const string LibraryAccessCallback = "lib:access";
    private const string LibraryCancelCallback = "lib:cancel";
    private const string BrandGree = "Gree";

    private static readonly IReadOnlyList<ManualSeriesOption> SupportedSeries =
    [
        new("gmv6", "Gree GMV6", "GMV6", "Gree GMV6 Service Manual EN.pdf"),
        new("gmv-mini", "Gree GMV Mini", "GMV Mini", "Gree GMV Mini Service Manual EN.pdf"),
        new("gmv-x", "Gree GMV X", "GMV X", "Gree GMV X Service Manual EN.pdf"),
        new("gmv9-flex", "Gree GMV9 Flex", "GMV9 Flex", "Gree GMV9 Flex Service Manual EN Rev B.pdf")
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
        string.Equals(callbackData, DiagnosticManualCallbackData, StringComparison.Ordinal);

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

        var replyMarkup = TelegramDiagnosticConversationService.DiagnosticManualContextKeyboard(access);
        var equipment = $"{last.Manufacturer} {series}";
        var binding = await _bindingStore.GetDiagnosticBySeriesAsync(BrandGree, series, cancellationToken);
        if (binding is null)
        {
            return Html(
                $"{TelegramHtml.Bold("Руководство пока не добавлено")}\n\n" +
                $"Для {TelegramHtml.Escape(equipment)} / {TelegramHtml.Escape(last.Code)} есть диагностическая карточка, " +
                "но безопасное Owner/User руководство для диагностики пока не привязано.",
                callbackAnswerText: "Руководство не добавлено",
                replyMarkup: replyMarkup);
        }

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
            return await BuildGreeCatalogAsync(access, cancellationToken);
        }

        if (string.Equals(callback, LibraryRemotesCallback, StringComparison.Ordinal))
        {
            return BuildEmptyLibrarySection("Пульты");
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

        if (callback.StartsWith("lib:file:", StringComparison.Ordinal))
        {
            return await SendLibraryFileAsync(callback["lib:file:".Length..], access, cancellationToken);
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

        _manualBindSessions[access.User.Id] = new ManualBindSession(ManualBindStage.SelectingSeries, null, null);

        return Task.FromResult(BindText(
            "Выберите серию Gree для привязки защищенного PDF-мануала.",
            SeriesKeyboard()));
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

        if (string.Equals(update.CallbackData, ManualBindCancel, StringComparison.Ordinal))
        {
            _manualBindSessions.TryRemove(access.User.Id, out _);
            return BindText("Привязка мануала отменена.", callbackAnswerText: "Отменено");
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

            _manualBindSessions[access.User.Id] = new ManualBindSession(ManualBindStage.WaitingForDocument, series, null);
            return BindText(
                BuildManualBindDocumentPrompt(series),
                CancelKeyboard(),
                "Пришлите PDF");
        }

        if (update.CallbackData is ManualBindConfirm or ManualBindReplace)
        {
            if (!_manualBindSessions.TryGetValue(access.User.Id, out var session) ||
                session.Series is null ||
                session.Candidate is null)
            {
                return BindText(
                    "Сессия привязки устарела. Запустите /manual_bind заново.",
                    callbackAnswerText: "Сессия устарела");
            }

            var binding = CreateSeriesBinding(session.Series, session.Candidate, access, update);
            await _bindingStore.UpsertSeriesAsync(binding, cancellationToken);
            _manualBindSessions.TryRemove(access.User.Id, out _);

            return BindText(
                $"Мануал привязан: {session.Series.DisplayName}. Файл: {session.Candidate.FileName}.",
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

        if (session.Series is null)
        {
            return BindText(
                "Выберите серию Gree для привязки защищенного PDF-мануала.",
                SeriesKeyboard());
        }

        var document = GetRegistrationDocument(update);
        if (document is null)
        {
            return BindText("Ожидаю PDF-файл мануала.", CancelKeyboard());
        }

        if (!IsValidManualBindDocument(document.Value, session.Series, out var reason))
        {
            return BindText(
                $"{reason}\n\nРекомендуемое имя: {session.Series.RecommendedFileName}",
                CancelKeyboard());
        }

        var candidate = new ManualBindCandidate(
            document.Value.FileId,
            document.Value.FileUniqueId,
            SafeFileName(document.Value.FileName) ?? session.Series.RecommendedFileName,
            SafeContentType(document.Value.MimeType),
            document.Value.FileSize,
            update.UserId ?? access.User.TelegramUserId,
            update.ChatId);
        var existing = await _bindingStore.GetBySeriesAsync(BrandGree, session.Series.Series, cancellationToken);
        var nextStage = existing is null
            ? ManualBindStage.WaitingForConfirmation
            : ManualBindStage.WaitingForReplaceConfirmation;

        _manualBindSessions[access.User.Id] = session with
        {
            Stage = nextStage,
            Candidate = candidate
        };

        var message = existing is null
            ? $"Подтвердите привязку {session.Series.DisplayName}: {candidate.FileName}."
            : $"Для {session.Series.DisplayName} уже есть активный файл: {SafeFileName(existing.OriginalFileName) ?? "без имени"}. Заменить на {candidate.FileName}?";

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
        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Пульты", LibraryRemotesCallback)]);

        if (CanManageLibrary(access.Role))
        {
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Запросы доступа", LibraryRequestsCallback)]);
            rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Управление доступом", LibraryAccessCallback)]);
        }

        rows.Add([new EquipmentDiagnosticTelegramInlineKeyboardButton("Отмена", LibraryCancelCallback)]);
        return BindText(
            "📚 Библиотека файлов\n\nВыберите раздел:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Библиотека");
    }

    private static TelegramManualLibraryResult BuildEmptyLibrarySection(string title) =>
        BindText(
            $"{title}\n\nВ этом разделе пока нет файлов.",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
            [
                [new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)]
            ]),
            "Нет файлов");

    private async Task<TelegramManualLibraryResult> BuildGreeCatalogAsync(
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var bindings = (await _bindingStore.ListAsync(cancellationToken))
            .Where(binding =>
                binding.IsActive &&
                binding.IsLibraryVisible &&
                string.Equals(binding.Brand, BrandGree, StringComparison.OrdinalIgnoreCase) &&
                CanAccessBinding(access, binding))
            .OrderBy(binding => binding.Series, StringComparer.OrdinalIgnoreCase)
            .ThenBy(binding => binding.Title ?? binding.OriginalFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (bindings.Length == 0)
        {
            return BindText(
                "В этом разделе пока нет доступных вам файлов.",
                new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard:
                [
                    [new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)]
                ]),
                "Нет файлов");
        }

        var rows = bindings
            .Select(binding => (IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>)
                [new EquipmentDiagnosticTelegramInlineKeyboardButton($"📄 {DisplayBindingTitle(binding)}", $"lib:file:{SeriesSlug(binding.Series)}")])
            .Append([new EquipmentDiagnosticTelegramInlineKeyboardButton("Назад", LibraryOpenCallback)])
            .ToArray();

        return BindText(
            "Gree\n\nДоступные файлы:",
            new EquipmentDiagnosticTelegramReplyMarkup(InlineKeyboard: rows),
            "Gree");
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
            return BindText("Новых запросов доступа нет.", callbackAnswerText: "Запросов нет");
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
        string seriesSlug,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var series = SupportedSeries.FirstOrDefault(item => SeriesSlug(item.Series) == seriesSlug);
        if (series is null)
        {
            return BindText("Файл не найден.", callbackAnswerText: "Не найден");
        }

        var binding = await _bindingStore.GetBySeriesAsync(BrandGree, series.Series, cancellationToken);
        if (binding is null || !binding.IsActive || !binding.IsLibraryVisible || !CanAccessBinding(access, binding))
        {
            return BindText("Файл недоступен.", callbackAnswerText: "Нет доступа");
        }

        var replyMarkup = TelegramDiagnosticConversationService.MainKeyboard(access);
        var text = $"Отправляю файл: {DisplayBindingTitle(binding)}";
        return new TelegramManualLibraryResult(
            text,
            [],
            [
                new EquipmentDiagnosticTelegramOutboundMessage(text, ReplyMarkup: replyMarkup),
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

    private static string SeriesSlug(string? value) =>
        NormalizeFileToken(value ?? string.Empty);

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

    private static string BuildManualBindDocumentPrompt(ManualSeriesOption series) =>
        $"Пришлите PDF-файл мануала для {series.DisplayName}.\n\nРекомендуемое имя: {series.RecommendedFileName}";

    private static bool IsValidManualBindDocument(
        RegistrationDocument document,
        ManualSeriesOption series,
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

        var normalized = NormalizeFileToken(fileName);
        if (!normalized.Contains("gree", StringComparison.Ordinal) ||
            !normalized.Contains(NormalizeFileToken(series.Series), StringComparison.Ordinal))
        {
            reason = "Имя файла должно содержать Gree и серию мануала.";
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

    private static TelegramManualFileBinding CreateSeriesBinding(
        ManualSeriesOption series,
        ManualBindCandidate candidate,
        TelegramUserAccessResult access,
        EquipmentDiagnosticTelegramUpdate update)
    {
        var now = update.ReceivedAt ?? DateTimeOffset.UtcNow;
        return new TelegramManualFileBinding(
            ManualId: SeriesManualId(series),
            TelegramFileId: candidate.FileId,
            OriginalFileName: candidate.FileName,
            ContentType: candidate.ContentType,
            RegisteredAtUtc: now,
            Source: "TelegramManualBind",
            RegisteredByRole: access.Role.ToString(),
            TelegramFileUniqueId: candidate.FileUniqueId,
            FileSize: candidate.FileSize,
            Brand: BrandGree,
            Series: series.Series,
            UploadedByTelegramUserId: candidate.UploadedByTelegramUserId,
            UploadedByTelegramChatId: candidate.UploadedByTelegramChatId,
            IsActive: true,
            UpdatedAtUtc: now,
            Title: candidate.FileName,
            DocumentType: TelegramLibraryDocumentType.ServiceManual,
            MinRole: TelegramUserRole.Engineer,
            IsLibraryVisible: true,
            CanUseForDiagnostics: false);
    }

    private static string SeriesManualId(ManualSeriesOption series) =>
        $"gree-{series.Slug}-service-manual";

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

    private sealed record ManualBindSession(
        ManualBindStage Stage,
        ManualSeriesOption? Series,
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
        SelectingSeries,
        WaitingForDocument,
        WaitingForConfirmation,
        WaitingForReplaceConfirmation
    }
}
