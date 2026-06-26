using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot.Routing;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.ServiceRequests;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Conversations;

public sealed class TelegramDiagnosticConversationService
{
    public const string NewCodeButton = "🔎 Новый код";
    public const string HistoryButton = "📋 История";
    public const string ServiceRequestButton = "🛠 Нужен мастер";
    public const string RequestsButton = "📄 Мои заявки";
    public const string SharePhoneButton = "📞 Поделиться номером Telegram";
    public const string ManualPhoneButton = "✏️ Ввести другой номер";
    public const string ChangePhoneButton = "✏️ Изменить номер";
    public const string CancelButton = "❌ Отмена";
    public const string UnknownButton = "Не знаю";

    private const int TelegramTechnicalChunkLength = 3500;
    private const int ConsumerMessageLength = 900;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromDays(30);
    private static readonly TimeSpan PendingServiceRequestTtl = TimeSpan.FromHours(2);
    private const string PendingServiceRequestActionType = "service-request";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record TelegramConversationSessionPayload(
        TelegramDiagnosticCandidate[] Candidates,
        TelegramConversationPendingAction? PendingAction);

    private sealed record TelegramConversationPendingAction(
        string Type,
        DateTimeOffset CreatedAt,
        DateTimeOffset ExpiresAt);

    private readonly ITelegramConversationSessionStore _sessionStore;
    private readonly IEquipmentDiagnosticsService _diagnosticsService;
    private readonly IEquipmentDiagnosticBotFacade _botFacade;
    private readonly IErrorKnowledgeLocalizationSource _localizedKnowledge;
    private readonly ITelegramUserStore _userStore;
    private readonly EquipmentDiagnosticTelegramMessageParser _parser;
    private readonly EquipmentDiagnosticTelegramResponseFormatter _formatter;
    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly TelegramDiagnosticHistoryService? _historyService;
    private readonly TelegramServiceRequestService? _serviceRequestService;

    public TelegramDiagnosticConversationService(
        ITelegramConversationSessionStore sessionStore,
        IEquipmentDiagnosticsService diagnosticsService,
        IEquipmentDiagnosticBotFacade botFacade,
        IErrorKnowledgeLocalizationSource localizedKnowledge,
        ITelegramUserStore userStore,
        EquipmentDiagnosticTelegramMessageParser parser,
        EquipmentDiagnosticTelegramResponseFormatter formatter,
        EquipmentDiagnosticTelegramOptions options,
        TelegramDiagnosticHistoryService? historyService = null,
        TelegramServiceRequestService? serviceRequestService = null)
    {
        _sessionStore = sessionStore;
        _diagnosticsService = diagnosticsService;
        _botFacade = botFacade;
        _localizedKnowledge = localizedKnowledge;
        _userStore = userStore;
        _parser = parser;
        _formatter = formatter;
        _options = options;
        _historyService = historyService;
        _serviceRequestService = serviceRequestService;
    }

    public static bool IsResetText(string? text)
    {
        var normalized = NormalizeText(text);
        return normalized is "новыйкод" or "new" or "reset" or "cancel" or "отмена";
    }

    public static bool IsManualPhoneText(string? text)
    {
        var normalized = NormalizeText(text);
        return normalized is "ввестидругойномер" or "изменитьномер" or "phone";
    }

    public static bool IsHistoryText(string? text)
    {
        var normalized = NormalizeText(text);
        return normalized is "история" or "history";
    }

    public static bool IsServiceRequestText(string? text)
    {
        var normalized = NormalizeText(text);
        return normalized is "нуженмастер" or "request";
    }

    public static bool IsRequestsText(string? text)
    {
        var normalized = NormalizeText(text);
        return normalized is "моизаявки" or "requests";
    }

    public async Task<TelegramDiagnosticConversationResult> ResetAsync(
        TelegramUserSnapshot user,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        await _sessionStore.ClearAsync(user.Id, cancellationToken);
        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Введите код ошибки, например: Gree H5.",
            [],
            MainKeyboard(access));
    }

    public async Task<TelegramDiagnosticConversationResult?> RepeatActivePromptAsync(
        TelegramUserSnapshot user,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetByTelegramUserIdAsync(user.Id, cancellationToken);
        if (session is null || session.State is TelegramConversationState.Idle or TelegramConversationState.ShowingResult)
        {
            return null;
        }

        var candidates = ReadCandidates(session.CandidateOptionsJson);
        var prompt = PromptForState(session, candidates, access);
        if (prompt.Handled)
        {
            return prompt;
        }

        if (session.State == TelegramConversationState.WaitingForPhoneNumber &&
            TryResolveDiagnosticPromptState(session, candidates, out var restoredState))
        {
            await SaveSessionAsync(session, restoredState, cancellationToken);
            return PromptForState(session with { State = restoredState }, candidates, access);
        }

        return null;
    }

    public async Task MarkPendingServiceRequestAsync(
        TelegramUserSnapshot user,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetByTelegramUserIdAsync(user.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(PendingServiceRequestTtl);
        var candidates = ReadCandidates(session?.CandidateOptionsJson);
        var payload = new TelegramConversationSessionPayload(
            candidates.ToArray(),
            new TelegramConversationPendingAction(PendingServiceRequestActionType, now, expiresAt));

        await _sessionStore.UpsertAsync(
            new TelegramConversationSessionUpsert(
                user.Id,
                TelegramConversationState.WaitingForPhoneNumber,
                session?.CurrentCode,
                session?.SelectedManufacturer,
                session?.SelectedEquipmentType,
                session?.SelectedDisplayContext,
                JsonSerializer.Serialize(payload, JsonOptions),
                session?.LastPromptMessageId,
                now,
                expiresAt),
            cancellationToken);
    }

    public async Task<TelegramDiagnosticConversationResult?> ResumePendingServiceRequestAfterPhoneSavedAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        if (_serviceRequestService is null || access.User is null)
        {
            return null;
        }

        var session = await _sessionStore.GetByTelegramUserIdAsync(access.User.Id, cancellationToken);
        if (session is null ||
            !TryReadPendingServiceRequest(session, update.ReceivedAt ?? DateTimeOffset.UtcNow, out var isExpired))
        {
            return null;
        }

        if (isExpired)
        {
            await _sessionStore.ClearAsync(access.User.Id, cancellationToken);
            return new TelegramDiagnosticConversationResult(
                true,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                "Спасибо, номер сохранён.\n\nСрок запроса на мастера истёк. Отправьте код ошибки ещё раз, например: Gree H5.",
                [],
                MainKeyboard(access));
        }

        var result = await _serviceRequestService.CreateFromLatestAsync(update, access, cancellationToken);
        await _sessionStore.ClearAsync(access.User.Id, cancellationToken);
        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            string.Join("\n\n", "Спасибо, номер сохранён.", result.Text),
            [],
            ServiceRequestKeyboard(result.Status, access));
    }

    public async Task<TelegramDiagnosticConversationResult> HandleTextAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        TelegramUserSnapshot user,
        CancellationToken cancellationToken)
    {
        if (IsResetText(update.Text))
        {
            return await ResetAsync(user, access, cancellationToken);
        }

        if (IsManualPhoneText(update.Text))
        {
            return await BeginPhoneInputAsync(user, access, cancellationToken);
        }

        if (IsHistoryText(update.Text))
        {
            return await FormatHistoryAsync(user, cancellationToken);
        }

        var session = await _sessionStore.GetByTelegramUserIdAsync(user.Id, cancellationToken);
        if (session?.State == TelegramConversationState.WaitingForPhoneNumber)
        {
            if (TryNormalizePhoneNumber(update.Text, out var phoneNumber))
            {
                return await SaveManualPhoneAsync(update, access, user, session, phoneNumber, cancellationToken);
            }

            if (_parser.TryExtractDiagnosticCode(update.Text, out var phoneCode))
            {
                return await StartFromCodeAsync(update, access, user, phoneCode, cancellationToken);
            }

            return PhoneValidationError();
        }

        if (_parser.TryExtractDiagnosticCode(update.Text, out var code))
        {
            return await StartFromCodeAsync(update, access, user, code, cancellationToken);
        }

        if (session is null)
        {
            return new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, []);
        }

        return session.State switch
        {
            TelegramConversationState.WaitingForBrand =>
                await SelectBrandAsync(session, access, update.Text, cancellationToken),
            TelegramConversationState.WaitingForEquipmentType =>
                await SelectEquipmentTypeAsync(session, access, update.Text, cancellationToken),
            TelegramConversationState.WaitingForDisplayContext =>
                await SelectDisplayContextAsync(session, access, update.Text, cancellationToken),
            _ => new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, [])
        };
    }

    private async Task<TelegramDiagnosticConversationResult> BeginPhoneInputAsync(
        TelegramUserSnapshot user,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.GetByTelegramUserIdAsync(user.Id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        await _sessionStore.UpsertAsync(
            new TelegramConversationSessionUpsert(
                user.Id,
                TelegramConversationState.WaitingForPhoneNumber,
                session?.CurrentCode,
                session?.SelectedManufacturer,
                session?.SelectedEquipmentType,
                session?.SelectedDisplayContext,
                session?.CandidateOptionsJson,
                session?.LastPromptMessageId,
                now,
                now.Add(SessionTtl)),
            cancellationToken);

        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Введите номер телефона для связи с сервисом.\nНапример: +998 90 123 45 67",
            [],
            PhoneInputKeyboard());
    }

    private async Task<TelegramDiagnosticConversationResult> SaveManualPhoneAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        TelegramUserSnapshot user,
        TelegramConversationSessionSnapshot session,
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        await _userStore.SavePhoneAsync(
            update.ChatId,
            phoneNumber,
            verified: false,
            TelegramUserPhoneNumberSource.Manual,
            update.ReceivedAt ?? DateTimeOffset.UtcNow,
            cancellationToken);

        var updatedUser = await _userStore.GetByChatIdAsync(update.ChatId, cancellationToken) ?? user;
        var updatedAccess = new TelegramUserAccessResult(
            access.IsAllowed,
            updatedUser,
            updatedUser.Role,
            access.DenialReason);

        var pendingRequestResult = await ResumePendingServiceRequestAfterPhoneSavedAsync(update, updatedAccess, cancellationToken);
        if (pendingRequestResult is not null)
        {
            return pendingRequestResult;
        }

        var resumePrompt = await ResumeDiagnosticPromptAsync(session, updatedAccess, cancellationToken);
        if (resumePrompt is not null)
        {
            return new TelegramDiagnosticConversationResult(
                true,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                string.Join("\n\n", _formatter.FormatPhoneSaved(_options.MaxMessageLength), "Продолжим диагностику.", resumePrompt.Text),
                resumePrompt.Warnings,
                resumePrompt.ReplyMarkup,
                resumePrompt.Messages);
        }

        await _sessionStore.ClearAsync(user.Id, cancellationToken);
        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            _formatter.FormatPhoneSaved(_options.MaxMessageLength),
            [],
            MainKeyboard(updatedAccess));
    }

    private async Task<TelegramDiagnosticConversationResult?> ResumeDiagnosticPromptAsync(
        TelegramConversationSessionSnapshot session,
        TelegramUserAccessResult access,
        CancellationToken cancellationToken)
    {
        var candidates = ReadCandidates(session.CandidateOptionsJson);
        if (!TryResolveDiagnosticPromptState(session, candidates, out var restoredState))
        {
            return null;
        }

        await SaveSessionAsync(session, restoredState, cancellationToken);
        return PromptForState(session with { State = restoredState }, candidates, access);
    }

    private async Task<TelegramDiagnosticConversationResult> StartFromCodeAsync(
        EquipmentDiagnosticTelegramUpdate update,
        TelegramUserAccessResult access,
        TelegramUserSnapshot user,
        string code,
        CancellationToken cancellationToken)
    {
        var parsedRequest = _parser.Parse(update.Text, _options).DiagnosticRequest;
        var requestedSeries = parsedRequest?.Series ?? DiagnosticRoutingHintExtractor.ExtractSeries(update.Text);
        var lookupCode = CanonicalizeVisualLookupCode(code);
        var candidates = await FindCandidatesByCodeAsync(lookupCode, requestedSeries, cancellationToken);
        if (!string.IsNullOrWhiteSpace(requestedSeries))
        {
            var seriesCandidates = candidates
                .Where(candidate => DiagnosticRoutingHintExtractor.MatchesSeries(candidate.Series, requestedSeries) &&
                                    !string.IsNullOrWhiteSpace(candidate.Series))
                .ToArray();
            if (seriesCandidates.Length > 0)
            {
                candidates = seriesCandidates;
            }
        }

        if (candidates.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(_options.DefaultManufacturer) &&
                EquipmentDiagnosticBotReferencePolicy.IsReferenceOnlyCode(code))
            {
                var diagnosis = await _botFacade.DiagnoseAsync(
                    new EquipmentDiagnosticBotRequest(
                        _options.DefaultManufacturer,
                        code,
                        FreeText: update.Text,
                        Series: requestedSeries,
                        PreferredLanguage: _options.PreferredLanguage),
                    cancellationToken);
                if (diagnosis.Status == EquipmentDiagnosticBotResponseStatus.NotFound)
                {
                    await RecordNotFoundAsync(access, conversationSessionId: null, code, _options.DefaultManufacturer, cancellationToken);
                    await _sessionStore.ClearAsync(user.Id, cancellationToken);
                    return NotFound(access, code);
                }

                await RecordCompletedAsync(
                    access,
                    conversationSessionId: null,
                    diagnosis,
                    _options.DefaultManufacturer,
                    code,
                    equipmentType: null,
                    displayContext: null,
                    candidateCount: 0,
                    cancellationToken);

                return FormatDiagnosis(diagnosis, access);
            }

            await _sessionStore.ClearAsync(user.Id, cancellationToken);
            await RecordNotFoundAsync(access, conversationSessionId: null, code, ExtractManufacturer(update.Text, code), cancellationToken);
            return NotFound(access, code);
        }

        if (HasCaseOnlyAmbiguityWithoutExactMatch(code, candidates))
        {
            await _sessionStore.ClearAsync(user.Id, cancellationToken);
            return new TelegramDiagnosticConversationResult(
                true,
                EquipmentDiagnosticTelegramResponseKind.ValidationError,
                "Код найден в нескольких вариантах регистра. Введите точный код с экрана оборудования без изменения букв: например D1 или d1.",
                [],
                MainKeyboard(access));
        }

        var selectedManufacturer = FindMentionedManufacturer(update.Text, candidates) ??
            SingleOrNull(candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase));

        return await ContinueAsync(
            user.Id,
            access,
            code,
            candidates,
            selectedManufacturer,
            selectedEquipmentType: null,
            selectedDisplayContext: null,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> SelectBrandAsync(
        TelegramConversationSessionSnapshot session,
        TelegramUserAccessResult access,
        string? text,
        CancellationToken cancellationToken)
    {
        var candidates = ReadCandidates(session.CandidateOptionsJson);
        var selected = MatchOption(text, candidates.Select(candidate => candidate.Manufacturer));
        if (selected is null)
        {
            return PromptForState(session, candidates, access);
        }

        return await ContinueAsync(
            session.TelegramUserId,
            access,
            session.CurrentCode ?? string.Empty,
            candidates,
            selected,
            session.SelectedEquipmentType,
            session.SelectedDisplayContext,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> SelectEquipmentTypeAsync(
        TelegramConversationSessionSnapshot session,
        TelegramUserAccessResult access,
        string? text,
        CancellationToken cancellationToken)
    {
        var candidates = ReadCandidates(session.CandidateOptionsJson);
        var selected = MatchEquipmentType(text);
        if (selected is null && !IsUnknown(text))
        {
            return PromptForState(session, candidates, access);
        }

        return await ContinueAsync(
            session.TelegramUserId,
            access,
            session.CurrentCode ?? string.Empty,
            candidates,
            session.SelectedManufacturer,
            selected,
            session.SelectedDisplayContext,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> SelectDisplayContextAsync(
        TelegramConversationSessionSnapshot session,
        TelegramUserAccessResult access,
        string? text,
        CancellationToken cancellationToken)
    {
        var candidates = ReadCandidates(session.CandidateOptionsJson);
        var selected = MatchDisplayContext(text);
        if (selected is null && !IsUnknown(text))
        {
            return PromptForState(session, candidates, access);
        }

        return await ContinueAsync(
            session.TelegramUserId,
            access,
            session.CurrentCode ?? string.Empty,
            candidates,
            session.SelectedManufacturer,
            session.SelectedEquipmentType,
            selected,
            cancellationToken);
    }

    private async Task<TelegramDiagnosticConversationResult> ContinueAsync(
        long telegramUserId,
        TelegramUserAccessResult access,
        string code,
        IReadOnlyList<TelegramDiagnosticCandidate> allCandidates,
        string? selectedManufacturer,
        string? selectedEquipmentType,
        string? selectedDisplayContext,
        CancellationToken cancellationToken)
    {
        var candidates = allCandidates;
        if (!string.IsNullOrWhiteSpace(selectedManufacturer))
        {
            candidates = candidates
                .Where(candidate => string.Equals(candidate.Manufacturer, selectedManufacturer, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(selectedEquipmentType))
        {
            candidates = candidates
                .Where(candidate => string.Equals(candidate.EquipmentType, selectedEquipmentType, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (!string.IsNullOrWhiteSpace(selectedDisplayContext))
        {
            candidates = candidates
                .Where(candidate => string.Equals(DisplayContextLabel(candidate.DisplayContext), selectedDisplayContext, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        if (candidates.Count == 0)
        {
            await RecordNotFoundAsync(access, conversationSessionId: null, code, selectedManufacturer, cancellationToken);
            return NotFound(access, code);
        }

        var resolveAsMeaningGroup = false;
        if (TrySelectSameMeaningCandidate(candidates, out var groupedCandidate))
        {
            resolveAsMeaningGroup = true;
            selectedManufacturer = groupedCandidate.Manufacturer;
            selectedEquipmentType = groupedCandidate.EquipmentType;
            selectedDisplayContext = DisplayContextLabel(groupedCandidate.DisplayContext);
            candidates = [groupedCandidate];
        }

        if (string.IsNullOrWhiteSpace(selectedManufacturer))
        {
            var manufacturers = candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (manufacturers.Length > 1)
            {
                await SaveAsync(telegramUserId, TelegramConversationState.WaitingForBrand, code, allCandidates, null, null, null, cancellationToken);
                return PromptBrand(manufacturers, access);
            }

            selectedManufacturer = manufacturers[0];
        }

        if (string.IsNullOrWhiteSpace(selectedEquipmentType))
        {
            var equipmentTypes = candidates.Select(candidate => candidate.EquipmentType).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (equipmentTypes.Length > 1)
            {
                await SaveAsync(telegramUserId, TelegramConversationState.WaitingForEquipmentType, code, allCandidates, selectedManufacturer, null, null, cancellationToken);
                return PromptEquipmentType(equipmentTypes, access);
            }

            selectedEquipmentType = equipmentTypes[0];
        }

        if (string.IsNullOrWhiteSpace(selectedDisplayContext))
        {
            var displayContexts = candidates.Select(candidate => DisplayContextLabel(candidate.DisplayContext)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (displayContexts.Length > 1)
            {
                await SaveAsync(telegramUserId, TelegramConversationState.WaitingForDisplayContext, code, allCandidates, selectedManufacturer, selectedEquipmentType, null, cancellationToken);
                return PromptDisplayContext(displayContexts, access);
            }

            selectedDisplayContext = displayContexts[0];
        }

        var session = await SaveAsync(
            telegramUserId,
            TelegramConversationState.ShowingResult,
            code,
            allCandidates,
            selectedManufacturer,
            selectedEquipmentType,
            selectedDisplayContext,
            cancellationToken);

        var finalCandidate = candidates[0];
        var requestCode = IsHoVisualAlias(code) &&
            string.Equals(finalCandidate.Code, "H0", StringComparison.OrdinalIgnoreCase)
                ? code
                : finalCandidate.Code;
        var diagnosis = await _botFacade.DiagnoseAsync(
            new EquipmentDiagnosticBotRequest(
                selectedManufacturer,
                requestCode,
                FreeText: null,
                Series: resolveAsMeaningGroup ? null : finalCandidate.Series,
                ModelCode: resolveAsMeaningGroup ? null : finalCandidate.ModelCode,
                Category: resolveAsMeaningGroup ? null : finalCandidate.Category,
                EquipmentSide: resolveAsMeaningGroup ? null : finalCandidate.EquipmentSide,
                DisplayContext: resolveAsMeaningGroup ? null : finalCandidate.DisplayContext,
                PreferredLanguage: _options.PreferredLanguage),
            cancellationToken);

        if (diagnosis.Status == EquipmentDiagnosticBotResponseStatus.NotFound)
        {
            await RecordNotFoundAsync(access, session.Id, code, selectedManufacturer, cancellationToken);
            return NotFound(access, code);
        }

        await RecordCompletedAsync(
            access,
            session.Id,
            diagnosis,
            selectedManufacturer,
            code,
            selectedEquipmentType,
            selectedDisplayContext,
            allCandidates.Count,
            cancellationToken);

        return FormatDiagnosis(diagnosis, access);
    }

    private async Task<IReadOnlyList<TelegramDiagnosticCandidate>> FindCandidatesByCodeAsync(
        string code,
        string? requestedSeries,
        CancellationToken cancellationToken)
    {
        var matches = await _diagnosticsService.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: null,
                ErrorCode: code,
                Series: null,
                ModelCode: null,
                Category: null),
            cancellationToken);

        var runtimeCandidates = matches
            .Select(match => new TelegramDiagnosticCandidate(
                match.Manufacturer,
                match.SeriesName,
                match.ModelCode,
                match.Code,
                match.Category,
                EquipmentTypeLabel(match.Category),
                EquipmentSide(match.Category),
                DisplayContext(match.Category),
                MeaningGroupId: null))
            .Distinct()
            .OrderBy(candidate => candidate.Manufacturer, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.EquipmentType, StringComparer.Ordinal)
            .ThenBy(candidate => DisplayContextLabel(candidate.DisplayContext), StringComparer.Ordinal)
            .ToArray();

        var localizedCandidates = _localizedKnowledge.GetEntries()
            .Where(entry => entry.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
            .Where(EquipmentDiagnosticBotReferencePolicy.IsSearchableLocalizedEntry)
            .Select(entry => new TelegramDiagnosticCandidate(
                entry.Manufacturer,
                entry.Series,
                entry.Models.Count == 1 ? entry.Models[0] : null,
                entry.Code,
                LocalizedCategory(entry.EquipmentType),
                LocalizedEquipmentTypeLabel(entry),
                LocalizedEquipmentSide(entry.EquipmentType),
                LocalizedDisplayContext(entry.DisplaySource),
                entry.MeaningGroupId))
            .Distinct()
            .OrderBy(candidate => candidate.Manufacturer, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Series ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.EquipmentType, StringComparer.Ordinal)
            .ThenBy(candidate => DisplayContextLabel(candidate.DisplayContext), StringComparer.Ordinal)
            .ToArray();

        TelegramDiagnosticCandidate[] exactSeriesLocalizedCandidates = string.IsNullOrWhiteSpace(requestedSeries) ||
            string.Equals(requestedSeries, "GMV", StringComparison.OrdinalIgnoreCase)
                ? []
                : localizedCandidates
                    .Where(candidate => DiagnosticRoutingHintExtractor.MatchesSeries(candidate.Series, requestedSeries) &&
                                        !string.IsNullOrWhiteSpace(candidate.Series))
                    .ToArray();
        if (exactSeriesLocalizedCandidates.Length > 0)
        {
            return PreferExactCodeMatches(code, exactSeriesLocalizedCandidates);
        }

        if (HasSameMeaningGroupCollision(localizedCandidates))
        {
            return PreferExactCodeMatches(code, localizedCandidates);
        }

        if (runtimeCandidates.Length > 0)
        {
            return PreferExactCodeMatches(code, runtimeCandidates);
        }

        return PreferExactCodeMatches(code, localizedCandidates);
    }

    private static string CanonicalizeVisualLookupCode(string code) =>
        IsHoVisualAlias(code) ? "H0" : code;

    private static bool IsHoVisualAlias(string code) =>
        string.Equals(code, "HO", StringComparison.OrdinalIgnoreCase);

    private async Task<TelegramConversationSessionSnapshot> SaveAsync(
        long telegramUserId,
        TelegramConversationState state,
        string code,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates,
        string? selectedManufacturer,
        string? selectedEquipmentType,
        string? selectedDisplayContext,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await _sessionStore.UpsertAsync(
            new TelegramConversationSessionUpsert(
                telegramUserId,
                state,
                code,
                selectedManufacturer,
                selectedEquipmentType,
                selectedDisplayContext,
                JsonSerializer.Serialize(candidates, JsonOptions),
                LastPromptMessageId: null,
                now,
                now.Add(SessionTtl)),
            cancellationToken);
    }

    private Task RecordCompletedAsync(
        TelegramUserAccessResult access,
        long? conversationSessionId,
        EquipmentDiagnosticBotResponse diagnosis,
        string? manufacturer,
        string code,
        string? equipmentType,
        string? displayContext,
        int? candidateCount,
        CancellationToken cancellationToken) =>
        _historyService is null
            ? Task.CompletedTask
            : _historyService.RecordCompletedAsync(
                access,
                conversationSessionId,
                diagnosis,
                manufacturer,
                code,
                equipmentType,
                displayContext,
                candidateCount,
                cancellationToken);

    private Task RecordNotFoundAsync(
        TelegramUserAccessResult access,
        long? conversationSessionId,
        string code,
        string? manufacturer,
        CancellationToken cancellationToken) =>
        _historyService is null
            ? Task.CompletedTask
            : _historyService.RecordNotFoundAsync(
                access,
                conversationSessionId,
                code,
                manufacturer,
                candidateCount: 0,
                cancellationToken);

    private async Task<TelegramDiagnosticConversationResult> FormatHistoryAsync(
        TelegramUserSnapshot user,
        CancellationToken cancellationToken)
    {
        var text = _historyService is null
            ? "История пока пустая. Отправьте код ошибки, например: Gree H5."
            : await _historyService.FormatHistoryAsync(user, cancellationToken);

        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            text,
            [],
            MainKeyboard(new TelegramUserAccessResult(true, user, user.Role)));
    }

    private async Task SaveSessionAsync(
        TelegramConversationSessionSnapshot session,
        TelegramConversationState state,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await _sessionStore.UpsertAsync(
            new TelegramConversationSessionUpsert(
                session.TelegramUserId,
                state,
                session.CurrentCode,
                session.SelectedManufacturer,
                session.SelectedEquipmentType,
                session.SelectedDisplayContext,
                session.CandidateOptionsJson,
                session.LastPromptMessageId,
                now,
                now.Add(SessionTtl)),
            cancellationToken);
    }

    private TelegramDiagnosticConversationResult FormatDiagnosis(
        EquipmentDiagnosticBotResponse diagnosis,
        TelegramUserAccessResult access)
    {
        if (access.UsesTechnicalResponse)
        {
            var technical = _formatter.FormatTechnical(diagnosis, access.Role);
            var messages = SplitTelegramMessage(technical)
                .Select(chunk => new EquipmentDiagnosticTelegramOutboundMessage(
                    chunk,
                    ParseMode: null,
                    DisableWebPagePreview: true,
                    MainKeyboard(access)))
                .ToArray();

            return new TelegramDiagnosticConversationResult(
                true,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                messages[0].Text,
                diagnosis.Warnings,
                messages[0].ReplyMarkup,
                messages);
        }

        var phoneSaved = access.User?.HasPhoneNumber == true;
        var text = _formatter.FormatConsumer(diagnosis, phoneSaved, ConsumerMessageLength);
        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            text,
            diagnosis.Warnings,
            MainKeyboard(access));
    }

    private TelegramDiagnosticConversationResult NotFound(
        TelegramUserAccessResult access,
        string code)
    {
        if (string.Equals(code, "01", StringComparison.Ordinal))
        {
            return new TelegramDiagnosticConversationResult(
                true,
                EquipmentDiagnosticTelegramResponseKind.Reply,
                "Код 01 не найден.\n\n" +
                "Возможно, вы имели в виду o1 — буква O + цифра 1.\n" +
                "Если на дисплее именно буква O, проверьте код o1.",
                [],
                MainKeyboard(access));
        }

        var text = "Я не нашёл точную расшифровку этого кода. Проверьте код или укажите бренд, например: Gree H5.";
        if (access.UsesTechnicalResponse)
        {
            text += "\n\nТехническая заметка: код не найден в рабочем диагностическом каталоге.";
        }

        return new TelegramDiagnosticConversationResult(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            text,
            [],
            MainKeyboard(access));
    }

    private TelegramDiagnosticConversationResult PromptForState(
        TelegramConversationSessionSnapshot session,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates,
        TelegramUserAccessResult access) =>
        session.State switch
        {
            TelegramConversationState.WaitingForBrand =>
                PromptBrand(candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), access),
            TelegramConversationState.WaitingForEquipmentType =>
                PromptEquipmentType(candidates.Select(candidate => candidate.EquipmentType).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), access),
            TelegramConversationState.WaitingForDisplayContext =>
                PromptDisplayContext(candidates.Select(candidate => DisplayContextLabel(candidate.DisplayContext)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(), access),
            _ => new TelegramDiagnosticConversationResult(false, EquipmentDiagnosticTelegramResponseKind.Unsupported, string.Empty, [])
        };

    private static bool TryResolveDiagnosticPromptState(
        TelegramConversationSessionSnapshot session,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates,
        out TelegramConversationState state)
    {
        state = TelegramConversationState.Idle;
        if (string.IsNullOrWhiteSpace(session.CurrentCode) || candidates.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(session.SelectedManufacturer) &&
            candidates.Select(candidate => candidate.Manufacturer).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            state = TelegramConversationState.WaitingForBrand;
            return true;
        }

        var manufacturerFiltered = string.IsNullOrWhiteSpace(session.SelectedManufacturer)
            ? candidates
            : candidates.Where(candidate => string.Equals(candidate.Manufacturer, session.SelectedManufacturer, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (string.IsNullOrWhiteSpace(session.SelectedEquipmentType) &&
            manufacturerFiltered.Select(candidate => candidate.EquipmentType).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            state = TelegramConversationState.WaitingForEquipmentType;
            return true;
        }

        var typeFiltered = string.IsNullOrWhiteSpace(session.SelectedEquipmentType)
            ? manufacturerFiltered
            : manufacturerFiltered.Where(candidate => string.Equals(candidate.EquipmentType, session.SelectedEquipmentType, StringComparison.OrdinalIgnoreCase)).ToArray();

        if (string.IsNullOrWhiteSpace(session.SelectedDisplayContext) &&
            typeFiltered.Select(candidate => DisplayContextLabel(candidate.DisplayContext)).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
        {
            state = TelegramConversationState.WaitingForDisplayContext;
            return true;
        }

        return false;
    }

    private static TelegramDiagnosticConversationResult PromptBrand(
        IReadOnlyList<string> manufacturers,
        TelegramUserAccessResult access) =>
        new(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Этот код встречается у нескольких брендов. Выберите бренд оборудования.",
            [],
            ChoiceKeyboard(manufacturers, access));

    private static TelegramDiagnosticConversationResult PromptEquipmentType(
        IReadOnlyList<string> equipmentTypes,
        TelegramUserAccessResult access) =>
        new(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Уточните тип оборудования, на котором показан код.",
            [],
            ChoiceKeyboard(equipmentTypes, access));

    private static TelegramDiagnosticConversationResult PromptDisplayContext(
        IReadOnlyList<string> displayContexts,
        TelegramUserAccessResult access) =>
        new(
            true,
            EquipmentDiagnosticTelegramResponseKind.Reply,
            "Уточните, где отображается код ошибки.",
            [],
            ChoiceKeyboard(displayContexts, access));

    public static EquipmentDiagnosticTelegramReplyMarkup MainKeyboard(TelegramUserAccessResult access)
    {
        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramKeyboardButton>>
        {
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(NewCodeButton) },
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(HistoryButton) },
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(ServiceRequestButton) },
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(RequestsButton) }
        };

        if (access.Role == TelegramUserRole.Consumer && access.User?.HasPhoneNumber != true)
        {
            rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(SharePhoneButton, RequestContact: true)]);
            rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(ManualPhoneButton)]);
        }
        else if (access.Role == TelegramUserRole.Consumer && access.User?.HasPhoneNumber == true)
        {
            rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(ChangePhoneButton)]);
        }
        if (access.UsesTechnicalResponse)
        {
            rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(TelegramManualLibraryService.ManualLibraryButton)]);
        }

        return new EquipmentDiagnosticTelegramReplyMarkup(rows, ResizeKeyboard: true, OneTimeKeyboard: false);
    }

    public static EquipmentDiagnosticTelegramReplyMarkup ServiceRequestCreatedKeyboard(
        TelegramUserAccessResult access)
    {
        var rows = new List<IReadOnlyList<EquipmentDiagnosticTelegramKeyboardButton>>
        {
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(NewCodeButton) },
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(HistoryButton) },
            new[] { new EquipmentDiagnosticTelegramKeyboardButton(RequestsButton) }
        };
        if (access.User?.HasPhoneNumber == true)
        {
            rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(ChangePhoneButton)]);
        }

        return new EquipmentDiagnosticTelegramReplyMarkup(rows, ResizeKeyboard: true, OneTimeKeyboard: false);
    }

    public static EquipmentDiagnosticTelegramReplyMarkup ServiceRequestPhoneKeyboard() =>
        new(
            [
                [new EquipmentDiagnosticTelegramKeyboardButton(SharePhoneButton, RequestContact: true)],
                [new EquipmentDiagnosticTelegramKeyboardButton(ManualPhoneButton)],
                [new EquipmentDiagnosticTelegramKeyboardButton(NewCodeButton)]
            ],
            ResizeKeyboard: true,
            OneTimeKeyboard: false);

    public static EquipmentDiagnosticTelegramReplyMarkup ServiceRequestKeyboard(
        TelegramServiceRequestAttemptStatus status,
        TelegramUserAccessResult access) =>
        status switch
        {
            TelegramServiceRequestAttemptStatus.PhoneMissing =>
                ServiceRequestPhoneKeyboard(),
            TelegramServiceRequestAttemptStatus.Created or TelegramServiceRequestAttemptStatus.Existing =>
                ServiceRequestCreatedKeyboard(access),
            _ => MainKeyboard(access)
        };

    private static EquipmentDiagnosticTelegramReplyMarkup ChoiceKeyboard(
        IReadOnlyList<string> options,
        TelegramUserAccessResult access)
    {
        var rows = options
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(option => option, StringComparer.Ordinal)
            .Select(option => (IReadOnlyList<EquipmentDiagnosticTelegramKeyboardButton>)[new EquipmentDiagnosticTelegramKeyboardButton(option)])
            .ToList();

        rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(UnknownButton)]);
        rows.Add([new EquipmentDiagnosticTelegramKeyboardButton(NewCodeButton)]);

        return new EquipmentDiagnosticTelegramReplyMarkup(rows, ResizeKeyboard: true, OneTimeKeyboard: false);
    }

    private static EquipmentDiagnosticTelegramReplyMarkup PhoneInputKeyboard() =>
        new(
            [
                [new EquipmentDiagnosticTelegramKeyboardButton(CancelButton)],
                [new EquipmentDiagnosticTelegramKeyboardButton(NewCodeButton)]
            ],
            ResizeKeyboard: true,
            OneTimeKeyboard: false);

    private static TelegramDiagnosticConversationResult PhoneValidationError() =>
        new(
            true,
            EquipmentDiagnosticTelegramResponseKind.ValidationError,
            "Не получилось распознать номер.\nВведите номер в формате: +998 90 123 45 67",
            [],
            PhoneInputKeyboard());

    private static bool TryNormalizePhoneNumber(
        string? text,
        out string phoneNumber)
    {
        phoneNumber = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        var startsWithPlus = trimmed.StartsWith('+');
        var compact = new string(trimmed.Where(character => character is not ' ' and not '\t' and not '\r' and not '\n' and not '(' and not ')' and not '-').ToArray());
        if (startsWithPlus)
        {
            compact = compact[1..];
        }

        if (compact.Contains('+', StringComparison.Ordinal) ||
            compact.Length == 0 ||
            compact.Any(character => !char.IsDigit(character)) ||
            compact.Length is < 7 or > 15)
        {
            return false;
        }

        phoneNumber = startsWithPlus ? $"+{compact}" : compact;
        return true;
    }

    private static IReadOnlyList<TelegramDiagnosticCandidate> ReadCandidates(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return JsonSerializer.Deserialize<TelegramDiagnosticCandidate[]>(json, JsonOptions) ?? [];
            }

            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array)
            {
                return candidates.Deserialize<TelegramDiagnosticCandidate[]>(JsonOptions) ?? [];
            }

            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool TryReadPendingServiceRequest(
        TelegramConversationSessionSnapshot session,
        DateTimeOffset now,
        out bool isExpired)
    {
        isExpired = false;
        if (session.State != TelegramConversationState.WaitingForPhoneNumber ||
            string.IsNullOrWhiteSpace(session.CandidateOptionsJson))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<TelegramConversationSessionPayload>(
                session.CandidateOptionsJson,
                JsonOptions);
            var pendingAction = payload?.PendingAction;
            if (pendingAction is null ||
                !string.Equals(pendingAction.Type, PendingServiceRequestActionType, StringComparison.Ordinal))
            {
                return false;
            }

            isExpired = pendingAction.ExpiresAt <= now ||
                (session.ExpiresAt is not null && session.ExpiresAt <= now);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> SplitTelegramMessage(string text)
    {
        if (text.Length <= TelegramTechnicalChunkLength)
        {
            return [text];
        }

        var chunks = new List<string>();
        var remaining = text;
        while (remaining.Length > TelegramTechnicalChunkLength)
        {
            var splitAt = remaining.LastIndexOf('\n', TelegramTechnicalChunkLength);
            if (splitAt < TelegramTechnicalChunkLength / 2)
            {
                splitAt = TelegramTechnicalChunkLength;
            }

            chunks.Add(remaining[..splitAt].Trim());
            remaining = remaining[splitAt..].Trim();
        }

        if (remaining.Length > 0)
        {
            chunks.Add(remaining);
        }

        return chunks;
    }

    private static string? FindMentionedManufacturer(
        string? text,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates)
    {
        var normalizedText = NormalizeText(text);
        return candidates
            .Select(candidate => candidate.Manufacturer)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(manufacturer => normalizedText.Contains(NormalizeText(manufacturer), StringComparison.OrdinalIgnoreCase));
    }

    private static string? ExtractManufacturer(string? text, string code)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var codeIndex = Array.FindIndex(tokens, token => string.Equals(token, code, StringComparison.OrdinalIgnoreCase));
        return codeIndex > 0 ? tokens[0] : null;
    }

    private static string? ExtractSeries(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(token => token.StartsWith("GMV", StringComparison.OrdinalIgnoreCase));
    }

    private static string? SingleOrNull(IEnumerable<string> values)
    {
        var distinct = values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return distinct.Length == 1 ? distinct[0] : null;
    }

    private static IReadOnlyList<TelegramDiagnosticCandidate> PreferExactCodeMatches(
        string requestedCode,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates)
    {
        var exact = candidates
            .Where(candidate => string.Equals(candidate.Code, requestedCode, StringComparison.Ordinal))
            .ToArray();
        return exact.Length > 0 ? exact : candidates;
    }

    private static bool TrySelectSameMeaningCandidate(
        IReadOnlyList<TelegramDiagnosticCandidate> candidates,
        out TelegramDiagnosticCandidate candidate)
    {
        candidate = default!;
        if (!HasSameMeaningGroupCollision(candidates))
        {
            return false;
        }

        candidate = candidates.FirstOrDefault(item => string.Equals(item.Series, "GMV6", StringComparison.OrdinalIgnoreCase)) ??
                    candidates[0];
        return true;
    }

    private static bool HasSameMeaningGroupCollision(IReadOnlyList<TelegramDiagnosticCandidate> candidates)
    {
        if (candidates.Count <= 1)
        {
            return false;
        }

        var meaningGroups = candidates
            .Select(item => item.MeaningGroupId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return meaningGroups.Length == 1 &&
               candidates.All(item => string.Equals(item.MeaningGroupId, meaningGroups[0], StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasCaseOnlyAmbiguityWithoutExactMatch(
        string requestedCode,
        IReadOnlyList<TelegramDiagnosticCandidate> candidates) =>
        candidates.Select(candidate => candidate.Code).Distinct(StringComparer.Ordinal).Count() > 1 &&
        candidates.Select(candidate => candidate.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1 &&
        candidates.All(candidate => !string.Equals(candidate.Code, requestedCode, StringComparison.Ordinal));

    private static string? MatchOption(string? text, IEnumerable<string> options)
    {
        if (IsUnknown(text))
        {
            return null;
        }

        var normalized = NormalizeText(text);
        return options.FirstOrDefault(option => NormalizeText(option) == normalized);
    }

    private static string? MatchEquipmentType(string? text)
    {
        var normalized = NormalizeText(text);
        return EquipmentTypeOptions()
            .FirstOrDefault(option => NormalizeText(option) == normalized);
    }

    private static string? MatchDisplayContext(string? text)
    {
        var normalized = NormalizeText(text);
        return DisplayContextOptions()
            .FirstOrDefault(option => NormalizeText(option) == normalized);
    }

    private static bool IsUnknown(string? text) =>
        string.Equals(NormalizeText(text), NormalizeText(UnknownButton), StringComparison.Ordinal);

    private static string NormalizeText(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : new string(text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static IReadOnlyList<string> EquipmentTypeOptions() =>
        ["Внутренний блок", "Наружный блок", "Чиллер", "Контроллер/пульт", "Сплит/полупром"];

    private static IReadOnlyList<string> DisplayContextOptions() =>
        ["Пульт", "Внутренний блок", "Наружный блок", "Приложение/шлюз", "Плата/LED"];

    private static string EquipmentTypeLabel(EquipmentCategory category) =>
        RussianDiagnosticTerminology.EquipmentTypeLabel(category);

    private static string LocalizedEquipmentTypeLabel(ErrorKnowledgeEntryV2 entry) =>
        entry.SignalType is ErrorKnowledgeSignalType.Debug or ErrorKnowledgeSignalType.Commissioning ||
        entry.PackageId.Contains("debugging", StringComparison.OrdinalIgnoreCase)
            ? RussianDiagnosticTerminology.SignalTypeLabel(ErrorKnowledgeSignalType.Commissioning)
            : entry.SignalType is ErrorKnowledgeSignalType.Status or ErrorKnowledgeSignalType.Maintenance
                ? RussianDiagnosticTerminology.SignalTypeLabel(ErrorKnowledgeSignalType.Status)
                : RussianDiagnosticTerminology.EquipmentTypeLabel(entry.EquipmentType);

    private static EquipmentCategory LocalizedCategory(ErrorKnowledgeEquipmentType equipmentType) =>
        equipmentType switch
        {
            ErrorKnowledgeEquipmentType.IndoorUnit => EquipmentCategory.VrfIndoorUnit,
            ErrorKnowledgeEquipmentType.WiredRemote or
            ErrorKnowledgeEquipmentType.CentralController or
            ErrorKnowledgeEquipmentType.Gateway => EquipmentCategory.Controller,
            ErrorKnowledgeEquipmentType.Chiller => EquipmentCategory.Chiller,
            _ => EquipmentCategory.VrfOutdoorUnit
        };

    private static EquipmentDiagnosticBotEquipmentSide LocalizedEquipmentSide(
        ErrorKnowledgeEquipmentType equipmentType) =>
        equipmentType switch
        {
            ErrorKnowledgeEquipmentType.IndoorUnit => EquipmentDiagnosticBotEquipmentSide.Indoor,
            ErrorKnowledgeEquipmentType.WiredRemote or
            ErrorKnowledgeEquipmentType.CentralController or
            ErrorKnowledgeEquipmentType.Gateway => EquipmentDiagnosticBotEquipmentSide.Controller,
            ErrorKnowledgeEquipmentType.Chiller => EquipmentDiagnosticBotEquipmentSide.Chiller,
            _ => EquipmentDiagnosticBotEquipmentSide.Outdoor
        };

    private static EquipmentDiagnosticBotDisplayContext LocalizedDisplayContext(
        ErrorKnowledgeDisplaySource displaySource) =>
        displaySource switch
        {
            ErrorKnowledgeDisplaySource.IndoorUnit => EquipmentDiagnosticBotDisplayContext.IduDisplay,
            ErrorKnowledgeDisplaySource.WiredRemote => EquipmentDiagnosticBotDisplayContext.WiredController,
            ErrorKnowledgeDisplaySource.CentralController => EquipmentDiagnosticBotDisplayContext.CentralizedController,
            ErrorKnowledgeDisplaySource.Gateway or ErrorKnowledgeDisplaySource.Software =>
                EquipmentDiagnosticBotDisplayContext.MobileAppOrGateway,
            _ => EquipmentDiagnosticBotDisplayContext.OduMainBoardLed
        };

    private static EquipmentDiagnosticBotEquipmentSide EquipmentSide(EquipmentCategory category) =>
        category switch
        {
            EquipmentCategory.VrfIndoorUnit => EquipmentDiagnosticBotEquipmentSide.Indoor,
            EquipmentCategory.VrfOutdoorUnit => EquipmentDiagnosticBotEquipmentSide.Outdoor,
            EquipmentCategory.Chiller => EquipmentDiagnosticBotEquipmentSide.Chiller,
            EquipmentCategory.Controller => EquipmentDiagnosticBotEquipmentSide.Controller,
            _ => EquipmentDiagnosticBotEquipmentSide.Unknown
        };

    private static EquipmentDiagnosticBotDisplayContext DisplayContext(EquipmentCategory category) =>
        category switch
        {
            EquipmentCategory.VrfIndoorUnit => EquipmentDiagnosticBotDisplayContext.IduDisplay,
            EquipmentCategory.VrfOutdoorUnit => EquipmentDiagnosticBotDisplayContext.OduMainBoardLed,
            EquipmentCategory.Controller => EquipmentDiagnosticBotDisplayContext.WiredController,
            _ => EquipmentDiagnosticBotDisplayContext.Unknown
        };

    private static string DisplayContextLabel(EquipmentDiagnosticBotDisplayContext context) =>
        context switch
        {
            EquipmentDiagnosticBotDisplayContext.WiredController => "Пульт",
            EquipmentDiagnosticBotDisplayContext.IduDisplay => "Внутренний блок",
            EquipmentDiagnosticBotDisplayContext.OduMainBoardLed => "Плата/LED",
            EquipmentDiagnosticBotDisplayContext.MobileAppOrGateway => "Приложение/шлюз",
            _ => "Не указано"
        };
}
