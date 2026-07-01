using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramResponseFormatter
{
    private const string RussianLocale = "ru";
    private const string CompactGreeSafety =
        "Не делайте вывод о неисправном компоненте только по одному коду. " +
        "Не обходите защиты. Работы с силовыми цепями и холодильным контуром выполняют квалифицированные специалисты.";
    private readonly IErrorKnowledgeLocalizationSource _localizationSource;

    public EquipmentDiagnosticTelegramResponseFormatter(
        IErrorKnowledgeLocalizationSource? localizationSource = null)
    {
        _localizationSource = localizationSource ?? new JsonErrorKnowledgeLocalizationSource();
    }

    public string Format(EquipmentDiagnosticBotResponse response, int maxLength) =>
        Truncate(FormatTechnical(response, TelegramUserRole.Engineer), maxLength);

    public string FormatTechnical(
        EquipmentDiagnosticBotResponse response,
        TelegramUserRole role = TelegramUserRole.Engineer)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Диагностика {response.NormalizedManufacturer} {response.NormalizedCode}");

        var localized = _localizationSource.Select(
            response,
            RussianLocale,
            Audience(role));
        if (localized is not null &&
            response.Status != EquipmentDiagnosticBotResponseStatus.ClarificationRequired)
        {
            AppendTechnicalAnswer(builder, response, localized);
            return NormalizeOutput(builder.ToString().Trim());
        }

        switch (response.Status)
        {
            case EquipmentDiagnosticBotResponseStatus.Answer:
                AppendMissingLocalizationFallback(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ClarificationRequired:
                AppendGenericSafety(builder);
                AppendTechnicalClarification(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ReferenceOnly:
                AppendGenericSafety(builder);
                builder.AppendLine("Справочное совпадение: это не подтвержденный диагноз неисправности.");
                builder.AppendLine("Рекомендованное действие: уточнить модель и место отображения кода по документации оборудования.");
                break;
            case EquipmentDiagnosticBotResponseStatus.NotFound:
                AppendGenericSafety(builder);
                builder.AppendLine("Код не найден в текущем проверенном каталоге. Проверьте производителя, семейство оборудования, место отображения кода и точную сервисную документацию.");
                builder.AppendLine("Рекомендованное действие: записать точную модель и проверить код по сервисному руководству.");
                break;
            case EquipmentDiagnosticBotResponseStatus.Unsupported:
            case EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope:
                AppendGenericSafety(builder);
                builder.AppendLine("Запрос вне поддерживаемого безопасного диагностического сценария.");
                builder.AppendLine("Рекомендованное действие: передать данные квалифицированному специалисту.");
                break;
        }

        return NormalizeOutput(builder.ToString().Trim());
    }

    public string FormatTechnicalHtml(
        EquipmentDiagnosticBotResponse response,
        TelegramUserRole role = TelegramUserRole.Engineer)
    {
        var localized = _localizationSource.Select(
            response,
            RussianLocale,
            Audience(role));
        if (localized is null ||
            response.Status == EquipmentDiagnosticBotResponseStatus.ClarificationRequired ||
            !string.Equals(localized.Entry.Manufacturer, "Gree", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(localized.Entry.Series))
        {
            return TelegramHtml.Escape(FormatTechnical(response, role));
        }

        var text = localized.Text;
        var entry = localized.Entry;
        var series = IsGroupedAnswer(response) ? "GMV" : entry.Series!;
        var meaning = TechnicalMeaning(response, text).TrimEnd('.');
        var builder = new StringBuilder();

        builder.AppendLine(TelegramHtml.Bold(
            $"Диагностика {response.NormalizedManufacturer} {response.NormalizedCode}"));
        builder.Append(TelegramHtml.Bold(
            $"{DisplayManufacturer(entry.Manufacturer)} {series} — {response.NormalizedCode}"));
        builder.AppendLine($" — {TelegramHtml.Escape(meaning)}");
        AppendHtmlConfusableCodeNote(builder, response);
        builder.AppendLine();
        builder.AppendLine(TelegramHtml.Bold("Суть:"));
        builder.AppendLine(TelegramHtml.Escape(
            RussianDiagnosticTerminology.ImprovePhrase(text.Summary)));
        AppendHtmlApplicableContexts(builder, response);
        AppendHtmlSection(builder, "Возможные причины:", text.PossibleCauses, numbered: false, limit: 5);
        AppendHtmlSection(
            builder,
            "Что проверить:",
            CompactChecks(response, entry, series),
            numbered: false,
            limit: 3);
        builder.AppendLine();
        builder.AppendLine($"{TelegramHtml.Bold("Серия:")} {TelegramHtml.Escape(series)}");
        builder.AppendLine();
        builder.AppendLine(TelegramHtml.Bold("Важно:"));
        builder.AppendLine(CompactGreeSafety);

        return builder.ToString().Trim();
    }

    private static IReadOnlyList<string> CompactChecks(
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeEntryV2 entry,
        string series)
    {
        var serviceProcedure = IsGroupedAnswer(response)
            ? "Дальнейшие действия выполняйте по сервисной процедуре руководства применимой серии."
            : "Дальнейшие действия выполняйте по сервисной процедуре для этой серии.";

        if (entry.SignalType is
            ErrorKnowledgeSignalType.Status or
            ErrorKnowledgeSignalType.Debug or
            ErrorKnowledgeSignalType.Commissioning or
            ErrorKnowledgeSignalType.Maintenance)
        {
            return
            [
                $"Подтвердите код {response.NormalizedCode}, категорию " +
                $"{RussianDiagnosticTerminology.SignalTypeLabel(entry.SignalType)} и место отображения.",
                "Сверьте модель, настройки и сопутствующие сообщения.",
                serviceProcedure
            ];
        }

        return
        [
            $"Подтвердите код {response.NormalizedCode}, серию {series} и место индикации.",
            "Сверьте модель, условия появления и сопутствующие коды.",
            serviceProcedure
        ];
    }

    public string FormatStart(int maxLength) =>
        Truncate(
            "AEngineer HVAC Service\n\n" +
            "Помогу быстро проверить код ошибки HVAC/VRF оборудования, открыть руководство или отправить заявку специалисту.\n\n" +
            "Напишите код ошибки, например:\n" +
            "Gree H5\n" +
            "Gree GMV6 HR U4\n" +
            "GMV Mini n2\n\n" +
            "Доступно:\n" +
            "🔎 диагностика по коду ошибки\n" +
            "📚 библиотека файлов\n" +
            "🛠 заявка специалисту\n" +
            "📋 история и мои заявки",
            maxLength);

    public string FormatHelp(int maxLength) =>
        Truncate(HelpText(), maxLength);

    public string FormatHelp(
        TelegramUserRole role,
        bool hasPhoneNumber,
        int maxLength) =>
        Truncate(HelpText(), maxLength);

    public string FormatConsumer(
        EquipmentDiagnosticBotResponse response,
        bool hasPhoneNumber,
        int maxLength)
    {
        var localized = _localizationSource.Select(
            response,
            RussianLocale,
            ErrorKnowledgeAudience.Consumer);
        if (localized is not null)
        {
            return FormatLocalizedConsumer(response, localized, hasPhoneNumber, maxLength);
        }

        var title = $"Внимание: ошибка {response.NormalizedManufacturer} {response.NormalizedCode}";
        var meaning = ConsumerMeaning(response);

        var builder = new StringBuilder();
        builder.AppendLine(title);
        builder.AppendLine();
        builder.AppendLine("Возможное значение:");
        builder.AppendLine(meaning);
        builder.AppendLine();
        builder.AppendLine("Что можно сделать безопасно:");
        builder.AppendLine("1. Выключите оборудование с пульта или панели управления.");
        builder.AppendLine("2. Подождите 3-5 минут и включите снова, если это безопасно.");
        builder.AppendLine("3. Если ошибка повторилась, не разбирайте блок самостоятельно.");
        builder.AppendLine();
        builder.AppendLine("Для сервиса:");
        builder.AppendLine($"Передайте мастеру код ошибки: {response.NormalizedManufacturer} {response.NormalizedCode}.");
        builder.AppendLine();
        builder.AppendLine(PhonePrompt(hasPhoneNumber));

        return TruncateConsumer(NormalizeOutput(builder.ToString().Trim()), maxLength);
    }

    private static string FormatLocalizedConsumer(
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeLocalizationSelection selection,
        bool hasPhoneNumber,
        int maxLength)
    {
        var text = selection.Text;
        var builder = new StringBuilder();
        builder.AppendLine($"Код оборудования: {response.NormalizedManufacturer} {response.NormalizedCode}");
        AppendConfusableCodeNote(builder, response);
        builder.AppendLine();
        builder.AppendLine(TechnicalTitle(response, text));
        builder.AppendLine();
        builder.AppendLine("Возможное значение:");
        builder.AppendLine(RussianDiagnosticTerminology.ImprovePhrase(text.Summary));
        AppendApplicableContexts(builder, response);
        builder.AppendLine();
        builder.AppendLine("Что можно сделать безопасно:");
        builder.AppendLine(RussianDiagnosticTerminology.ImprovePhrase(text.SafetyNote));
        AppendSection(builder, "Возможные причины", text.PossibleCauses);
        AppendSection(builder, "Что можно проверить безопасно", text.CheckSteps);
        builder.AppendLine();
        builder.AppendLine("Рекомендованное действие:");
        builder.AppendLine(RecommendedAction(response, text));
        builder.AppendLine();
        builder.AppendLine($"Для сервиса: передайте код {response.NormalizedManufacturer} {response.NormalizedCode}.");
        builder.AppendLine();
        builder.AppendLine(PhonePrompt(hasPhoneNumber));
        return TruncateConsumer(NormalizeOutput(builder.ToString().Trim()), maxLength);
    }

    public string FormatMe(
        TelegramUserSnapshot? user,
        int maxLength)
    {
        if (user is null)
        {
            return Truncate("Профиль Telegram пока недоступен.", maxLength);
        }

        return Truncate(
            "Ваш доступ Telegram\n" +
            $"Роль: {RoleName(user.Role)}\n" +
            $"Доступ: {(user.IsEnabled ? "включен" : "выключен")}\n" +
            $"Блокировка: {(user.IsBlocked ? "да" : "нет")}\n" +
            $"Телефон: {(user.HasPhoneNumber ? "сохранен" : "не сохранен")}",
            maxLength);
    }

    public string FormatAdminHelp(int maxLength) =>
        Truncate(
            "Админ-команды\n" +
            "/admin_users — управление пользователями кнопками\n" +
            "/admin_pending — новые пользователи\n" +
            "/admin_audit — последние события управления пользователями\n" +
            "/engineers — сервис-инженеры\n\n" +
            "Роли: Владелец, Администратор, Сервис-инженер, Монтажник, Клиент.\n" +
            "Монтажник получает технические объяснения без доступа к сервисной очереди.\n\n" +
            "Очередь: /queue [active|new|in-progress|closed|all]\n" +
            "Мои назначенные заявки: /my_requests\n" +
            "История сервисной заявки: /request_events <id>\n\n" +
            "Команды с параметрами (fallback):\n" +
            "/admin users\n" +
            "/admin allow <chatId>\n" +
            "/admin block <chatId>\n" +
            "/admin unblock <chatId>\n" +
            "/admin disable <chatId>\n" +
            "/admin enable <chatId>\n" +
            "/admin role <chatId> <Owner|Admin|Engineer|Installer|Consumer>",
            maxLength);

    public string FormatAdminUsers(
        IReadOnlyList<TelegramUserSnapshot> users,
        int maxLength)
    {
        if (users.Count == 0)
        {
            return "Пользователей Telegram пока нет.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Пользователи Telegram");
        foreach (var user in users)
        {
            var phone = user.HasPhoneNumber
                ? user.PhoneNumberSource switch
                {
                    TelegramUserPhoneNumberSource.Manual => "да(manual)",
                    TelegramUserPhoneNumberSource.TelegramContact => "да(telegram)",
                    _ => "да"
                }
                : "нет";
            builder.AppendLine(
                $"{user.TelegramChatId}: {RoleName(user.Role)}; доступ={(user.IsEnabled ? "включен" : "выключен")}; блокировка={(user.IsBlocked ? "да" : "нет")}; телефон={phone}");
        }

        return Truncate(builder.ToString().Trim(), maxLength);
    }

    public string FormatPhoneSaved(int maxLength) =>
        Truncate("Спасибо, номер сохранен. Теперь отправьте код ошибки, например: Gree H5.", maxLength);

    public string FormatValidation(IReadOnlyList<string> errors, int maxLength) =>
        Truncate($"Запрос не принят. {string.Join(" ", errors.Select(TranslateValidationError))} Напишите /help для примеров.", maxLength);

    public string FormatUnsupported(int maxLength) =>
        Truncate(
            "Команда или модель контроллера не поддерживается. Напишите /help или отправьте производителя и код ошибки.",
            maxLength);

    public string FormatIdentity(EquipmentDiagnosticTelegramUpdate update, int maxLength) =>
        Truncate(
            "Идентификатор Telegram-доступа\n" +
            $"chatId: {update.ChatId}\n" +
            $"userId: {update.UserId?.ToString() ?? "нет"}\n" +
            $"username: {update.Username ?? "нет"}\n" +
            "Добавьте chatId в allowlist или BootstrapOwnerChatId и отключите режим discovery.",
            maxLength);

    private static string ConsumerMeaning(EquipmentDiagnosticBotResponse response) =>
        response.Status switch
        {
            EquipmentDiagnosticBotResponseStatus.Answer =>
                "Сработала защита оборудования. Точное значение зависит от модели и места отображения ошибки.",
            EquipmentDiagnosticBotResponseStatus.ClarificationRequired =>
                "Нужно уточнить, где показан код: на пульте, внутреннем блоке или наружном блоке.",
            EquipmentDiagnosticBotResponseStatus.ReferenceOnly =>
                "Код похож на справочное указание. Это не подтвержденный диагноз неисправности.",
            EquipmentDiagnosticBotResponseStatus.NotFound =>
                "Код не найден в текущем проверенном каталоге. Проверьте точный бренд и код на дисплее.",
            EquipmentDiagnosticBotResponseStatus.Unsupported or EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope =>
                "Запрос выходит за рамки безопасной простой расшифровки.",
            _ => "Требуется проверка по точной модели оборудования."
        };

    private static string TranslateValidationError(string error) =>
        error switch
        {
            "Message text is required." => "Нужен текст сообщения или контакт.",
            var value when value.StartsWith("Message must contain at most ", StringComparison.Ordinal) =>
                "Сообщение слишком длинное.",
            "Message contains unsupported control characters." =>
                "Сообщение содержит неподдерживаемые управляющие символы.",
            "Manufacturer and displayed code are required after /diagnose." =>
                "После /diagnose укажите производителя и код ошибки.",
            "A displayed diagnostic code is required." =>
                "Укажите код ошибки, например: Gree H5.",
            var value when value.StartsWith("Manufacturer is required", StringComparison.Ordinal) =>
                "Укажите производителя, например: Gree H5.",
            _ => error
        };

    private static void AppendTechnicalAnswer(
        StringBuilder builder,
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeLocalizationSelection selection)
    {
        var text = selection.Text;
        var entry = selection.Entry;
        if (string.Equals(entry.Manufacturer, "Gree", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(entry.Series))
        {
            AppendPolishedGreeAnswer(builder, response, selection);
            return;
        }

        builder.AppendLine(TechnicalTitle(response, text));
        AppendConfusableCodeNote(builder, response);
        builder.AppendLine();
        builder.AppendLine("Суть:");
        builder.AppendLine(RussianDiagnosticTerminology.ImprovePhrase(text.Summary));
        AppendApplicableContexts(builder, response);
        if (!text.IsReviewed || !IsVerified(entry.VerificationStatus))
        {
            builder.AppendLine();
            builder.AppendLine("Примечание: текст нужно сверить с документацией установленной модели.");
        }
        AppendCheckSection(builder, text);
        AppendImportantSection(builder, text.SafetyNote);
        AppendSection(builder, "Ограничения вывода", text.DoNotAdvise);
        builder.AppendLine();
        builder.AppendLine("Дальше:");
        builder.AppendLine(RecommendedAction(response, text));
    }

    private static void AppendPolishedGreeAnswer(
        StringBuilder builder,
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeLocalizationSelection selection)
    {
        var text = selection.Text;
        var entry = selection.Entry;
        var series = IsGroupedAnswer(response) ? "GMV" : entry.Series!;

        builder.Clear();
        builder.AppendLine($"{DisplayManufacturer(entry.Manufacturer)} {series} — {response.NormalizedCode}");
        AppendConfusableCodeNote(builder, response);
        builder.AppendLine();
        builder.AppendLine("Значение:");
        var meaning = TechnicalMeaning(response, text);
        var summary = RussianDiagnosticTerminology.ImprovePhrase(text.Summary);
        builder.AppendLine(meaning);
        if (!string.Equals(meaning, summary, StringComparison.OrdinalIgnoreCase))
        {
            builder.AppendLine(summary);
        }
        AppendApplicableContexts(builder, response);
        AppendSection(builder, "Возможные причины", text.PossibleCauses);
        AppendNumberedSection(builder, "Первые проверки", text.CheckSteps, 3);
        builder.AppendLine();
        builder.AppendLine($"Серия: {series}");
        AppendImportantSection(builder, text.SafetyNote);
        AppendSection(builder, "Ограничения вывода", text.DoNotAdvise);
        builder.AppendLine();
        builder.AppendLine("Дальше:");
        builder.AppendLine(RecommendedAction(response, text));
    }

    private static string TechnicalMeaning(
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeTextV2 text)
    {
        var title = TechnicalTitle(response, text);
        var titleParts = title.Split('—', StringSplitOptions.TrimEntries);
        for (var index = 0; index < titleParts.Length - 1; index++)
        {
            if (!string.Equals(titleParts[index], response.NormalizedCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var meaningAfterCode = string.Join(" — ", titleParts[(index + 1)..]).Trim();
            if (!string.IsNullOrWhiteSpace(meaningAfterCode))
            {
                return NormalizeTechnicalMeaning(meaningAfterCode);
            }
        }

        var separator = title.IndexOf('—', StringComparison.Ordinal);
        var separatorLength = 1;
        if (separator < 0)
        {
            separator = title.IndexOf(" - ", StringComparison.Ordinal);
            separatorLength = 3;
        }

        if (separator < 0 || separator + separatorLength >= title.Length)
        {
            return RussianDiagnosticTerminology.ImprovePhrase(text.Summary);
        }

        return NormalizeTechnicalMeaning(title[(separator + separatorLength)..]);
    }

    private static string NormalizeTechnicalMeaning(string value)
    {
        var meaning = value.Trim();
        return RussianDiagnosticTerminology.ImprovePhrase(
            string.Concat(char.ToUpperInvariant(meaning[0]), meaning[1..].TrimEnd('.'), "."));
    }

    private static string TechnicalTitle(
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeTextV2 text) =>
        IsGroupedAnswer(response)
            ? $"{DisplayManufacturer(response.NormalizedManufacturer)} GMV {response.NormalizedCode} — нарушение связи"
            : RussianDiagnosticTerminology.ImprovePhrase(text.Title);

    private static string RecommendedAction(
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeTextV2 text) =>
        IsGroupedAnswer(response)
            ? $"Продолжить диагностику по разделу {response.NormalizedCode} руководства применимой серии. Если серия известна — использовать руководство этой серии."
            : RussianDiagnosticTerminology.ImprovePhrase(text.RecommendedAction);

    private static bool IsGroupedAnswer(EquipmentDiagnosticBotResponse response) =>
        response.ApplicableContexts.Count > 1;

    private static string DisplayManufacturer(string manufacturer) =>
        string.Equals(manufacturer, "GREE", StringComparison.OrdinalIgnoreCase)
            ? "Gree"
            : manufacturer;

    private static void AppendConfusableCodeNote(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        var code = response.NormalizedCode;
        if (string.Equals(code, "o1", StringComparison.Ordinal))
        {
            builder.AppendLine("Код: o1 — буква O + цифра 1.");
        }
        else if (string.Equals(code, "L1", StringComparison.Ordinal))
        {
            builder.AppendLine("Код: L1 — буква L + цифра 1.");
        }
        else if (string.Equals(code, "H0", StringComparison.Ordinal) &&
            IsObservedHoAlias(response.ObservedCode.Code))
        {
            builder.AppendLine("Проверьте точное написание: на семисегментном дисплее HO/Ho часто означает H0.");
        }
    }

    private static bool IsObservedHoAlias(string observedCode) =>
        string.Equals(observedCode.Trim(), "HO", StringComparison.OrdinalIgnoreCase);

    private static void AppendHtmlConfusableCodeNote(
        StringBuilder builder,
        EquipmentDiagnosticBotResponse response)
    {
        var note = ConfusableCodeNote(response);
        if (note is not null)
        {
            builder.AppendLine(TelegramHtml.Escape(note));
        }
    }

    private static string? ConfusableCodeNote(EquipmentDiagnosticBotResponse response)
    {
        var code = response.NormalizedCode;
        if (string.Equals(code, "o1", StringComparison.Ordinal))
        {
            return "Код: o1 — буква O + цифра 1.";
        }

        if (string.Equals(code, "L1", StringComparison.Ordinal))
        {
            return "Код: L1 — буква L + цифра 1.";
        }

        if (string.Equals(code, "H0", StringComparison.Ordinal) &&
            IsObservedHoAlias(response.ObservedCode.Code))
        {
            return "Проверьте точное написание: на семисегментном дисплее HO/Ho часто означает H0.";
        }

        return null;
    }

    private static void AppendApplicableContexts(
        StringBuilder builder,
        EquipmentDiagnosticBotResponse response)
    {
        if (response.ApplicableContexts.Count <= 1)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Применимо:");
        foreach (var context in response.ApplicableContexts.Take(6))
        {
            builder.AppendLine($"- {context}");
        }
    }

    private static void AppendHtmlApplicableContexts(
        StringBuilder builder,
        EquipmentDiagnosticBotResponse response)
    {
        if (response.ApplicableContexts.Count <= 1)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine(TelegramHtml.Bold("Применимо:"));
        foreach (var context in response.ApplicableContexts.Take(6))
        {
            builder.AppendLine($"- {TelegramHtml.Escape(context)}");
        }
    }

    private static void AppendMissingLocalizationFallback(
        StringBuilder builder,
        EquipmentDiagnosticBotResponse response)
    {
        builder.AppendLine("Суть:");
        builder.AppendLine("Техническое описание для этого кода пока не локализовано.");
        AppendGenericSafety(builder);
        if (response.IsSeedKnowledge || response.VerificationRequired)
        {
            builder.AppendLine("Черновик / непроверено: проверьте точное значение по сервисному руководству установленной модели.");
        }
    }

    private static void AppendTechnicalClarification(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        if (response.ClarificationQuestion is null)
        {
            return;
        }

        builder.AppendLine("Код найден для нескольких вариантов оборудования. Уточните установленный контекст:");
        foreach (var option in response.ClarificationQuestion.Options)
        {
            var family = string.IsNullOrWhiteSpace(option.Series)
                ? option.Manufacturer
                : $"{option.Manufacturer} {option.Series}";
            builder.AppendLine($"- {family}: укажите контекст «{EquipmentSideLabel(option.EquipmentSide)}».");
        }
    }

    private static void AppendCheckSection(
        StringBuilder builder,
        ErrorKnowledgeTextV2 text)
    {
        var values = text.PossibleCauses.Concat(text.CheckSteps).ToArray();
        AppendSection(builder, "Что проверить", values);
    }

    private static void AppendImportantSection(
        StringBuilder builder,
        string safetyNote)
    {
        if (string.IsNullOrWhiteSpace(safetyNote))
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Важно:");
        builder.AppendLine(RussianDiagnosticTerminology.ImprovePhrase(safetyNote));
    }

    private static void AppendSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"{title}:");
        foreach (var value in values.Take(5))
        {
            builder.AppendLine($"- {Compact(RussianDiagnosticTerminology.ImprovePhrase(value), 220)}");
        }
    }

    private static void AppendNumberedSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<string> values,
        int limit)
    {
        if (values.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine($"{title}:");
        foreach (var (value, index) in values.Take(limit).Select((value, index) => (value, index)))
        {
            builder.AppendLine($"{index + 1}. {Compact(RussianDiagnosticTerminology.ImprovePhrase(value), 220)}");
        }
    }

    private static void AppendHtmlSection(
        StringBuilder builder,
        string title,
        IReadOnlyList<string> values,
        bool numbered,
        int limit)
    {
        if (values.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine(TelegramHtml.Bold(title));
        foreach (var (value, index) in values.Take(limit).Select((value, index) => (value, index)))
        {
            var marker = numbered ? $"{index + 1}." : "-";
            builder.AppendLine(
                $"{marker} {TelegramHtml.Escape(Compact(RussianDiagnosticTerminology.ImprovePhrase(value), 220))}");
        }
    }

    private static void AppendGenericSafety(StringBuilder builder)
    {
        builder.AppendLine("Важно: электрические, компрессорные, инверторные и холодильные проверки выполняет квалифицированный специалист. Повторные пуски до проверки не выполнять.");
    }

    private static ErrorKnowledgeAudience Audience(TelegramUserRole role) =>
        role == TelegramUserRole.Installer
            ? ErrorKnowledgeAudience.Installer
            : ErrorKnowledgeAudience.Engineer;

    private static bool IsVerified(string verificationStatus) =>
        verificationStatus is "ManualVerified" or "Verified" or "Reviewed";

    private static string EquipmentSideLabel(EquipmentDiagnosticBotEquipmentSide side) =>
        side switch
        {
            EquipmentDiagnosticBotEquipmentSide.Indoor => "внутренний блок",
            EquipmentDiagnosticBotEquipmentSide.Outdoor => "наружный блок",
            EquipmentDiagnosticBotEquipmentSide.Chiller => "чиллер",
            EquipmentDiagnosticBotEquipmentSide.Controller => "контроллер",
            EquipmentDiagnosticBotEquipmentSide.CommissioningTool => "сервисный инструмент",
            _ => "неуточнённое оборудование"
        };

    private static string PhonePrompt(bool hasPhoneNumber) =>
        hasPhoneNumber
            ? "Ваш номер уже сохранен."
            : "Если хотите, оставьте номер телефона:\n" +
              "- можно поделиться номером Telegram\n" +
              "- или ввести другой номер для звонка.";

    private static string HelpText() =>
        "Как пользоваться AEngineer HVAC Service\n\n" +
        "Введите код ошибки или модель с кодом:\n" +
        "Gree H5\n" +
        "Gree GMV6 HR U4\n" +
        "GMV Mini n2\n\n" +
        "Если код найден в нескольких сериях, бот предложит выбрать нужную.\n\n" +
        "После диагностики можно:\n" +
        "📘 открыть руководство пользователя\n" +
        "🛠 оставить заявку специалисту\n" +
        "📋 посмотреть историю\n\n" +
        "Также доступна 📚 Библиотека файлов — там можно открыть руководства по сериям, внутренним блокам, пультам и другим разделам.\n\n" +
        "Команды:\n" +
        "/history — история диагностик\n" +
        "/last — последняя диагностика\n" +
        "/start — главное меню";

    private static string RoleName(TelegramUserRole role) =>
        TelegramUserRolePolicy.DisplayName(role);

    private static string Compact(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3).TrimEnd(), "...");

    private static string NormalizeOutput(string text) =>
        RussianDiagnosticTerminology.ImprovePhrase(text);

    private static string TruncateConsumer(string text, int maxLength)
    {
        var effectiveMax = Math.Max(320, maxLength);
        if (text.Length <= effectiveMax)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, effectiveMax - 3).TrimEnd(), "...");
    }

    private static string Truncate(string text, int maxLength)
    {
        var effectiveMax = Math.Max(80, maxLength);
        if (text.Length <= effectiveMax)
        {
            return text;
        }

        const string suffix = "\n[Ответ сокращен.]";
        return string.Concat(text.AsSpan(0, effectiveMax - suffix.Length).TrimEnd(), suffix);
    }
}
