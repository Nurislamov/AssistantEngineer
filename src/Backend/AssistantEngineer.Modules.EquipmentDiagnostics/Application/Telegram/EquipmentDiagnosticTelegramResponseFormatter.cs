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
            return builder.ToString().Trim();
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

        return builder.ToString().Trim();
    }

    public string FormatHelp(int maxLength) =>
        Truncate(
            "Диагностика оборудования\n" +
            "Напишите производителя и код ошибки, например: Gree H5.\n" +
            "Можно добавить контекст: Gree C5 наружный блок; Gree C5 внутренний блок; /diagnose Gree H5.\n" +
            "Подсказка основана на проверенном каталоге и требует сверки с точной моделью и сервисной документацией.",
            maxLength);

    public string FormatHelp(
        TelegramUserRole role,
        bool hasPhoneNumber,
        int maxLength)
    {
        if (role == TelegramUserRole.Consumer)
        {
            return Truncate(
                "Диагностика оборудования\n\n" +
                "Напишите код ошибки, например:\n" +
                "Gree H5\n\n" +
                "Я покажу простую и безопасную расшифровку, которую можно передать сервисному специалисту.\n\n" +
                "История: /history, последняя диагностика: /last.\n\n" +
                PhonePrompt(hasPhoneNumber),
                maxLength);
        }

        if (role == TelegramUserRole.Installer)
        {
            return Truncate(
                "Диагностика оборудования\n\n" +
                "Напишите производителя и код ошибки, например: Gree H5.\n" +
                "Доступны технические объяснения, история /history и последняя диагностика /last.\n" +
                "Роль «Монтажник» не даёт доступа к сервисной очереди, контактам клиентов или админ-командам.\n" +
                "Технический текст выводится по-русски; непереведённые записи показываются безопасным русским сообщением.",
                maxLength);
        }

        var adminLine = TelegramUserRolePolicy.IsAdminRole(role)
            ? "\n\nАдмин-команды: /admin_help"
            : string.Empty;

        return Truncate(
            "Диагностика оборудования\n\n" +
            "Напишите производителя и код ошибки, например: Gree H5.\n" +
            "Можно добавить контекст: Gree C5 outdoor; Gree C5 indoor; /diagnose Gree H5.\n" +
            "История: /history, последняя диагностика: /last.\n" +
            "Сервисная очередь: /queue, мои назначенные заявки: /my_requests. " +
            "Действия: /take, /done, /cancel_request, /contact.\n" +
            "Техническая подсказка требует проверки по точной модели и сервисной документации." +
            adminLine,
            maxLength);
    }

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
            return FormatLocalizedConsumer(response, localized.Text, hasPhoneNumber, maxLength);
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

        return TruncateConsumer(builder.ToString().Trim(), maxLength);
    }

    private static string FormatLocalizedConsumer(
        EquipmentDiagnosticBotResponse response,
        ErrorKnowledgeTextV2 text,
        bool hasPhoneNumber,
        int maxLength)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Внимание: ошибка {response.NormalizedManufacturer} {response.NormalizedCode}");
        builder.AppendLine();
        builder.AppendLine(text.Title);
        builder.AppendLine();
        builder.AppendLine("Возможное значение:");
        builder.AppendLine(text.Summary);
        builder.AppendLine();
        builder.AppendLine("Что можно сделать безопасно:");
        builder.AppendLine(text.SafetyNote);
        AppendSection(builder, "Возможные причины", text.PossibleCauses);
        AppendSection(builder, "Что можно проверить безопасно", text.CheckSteps);
        builder.AppendLine();
        builder.AppendLine("Рекомендованное действие:");
        builder.AppendLine(text.RecommendedAction);
        builder.AppendLine();
        builder.AppendLine($"Для сервиса: передайте код {response.NormalizedManufacturer} {response.NormalizedCode}.");
        builder.AppendLine();
        builder.AppendLine(PhonePrompt(hasPhoneNumber));
        return TruncateConsumer(builder.ToString().Trim(), maxLength);
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
        builder.AppendLine(text.Title);
        builder.AppendLine();
        builder.AppendLine("Кратко:");
        builder.AppendLine(text.Summary);
        builder.AppendLine();
        builder.AppendLine("Безопасность:");
        builder.AppendLine(text.SafetyNote);
        builder.AppendLine();
        builder.AppendLine($"Уверенность: {ConfidenceLabel(entry.Confidence)}.");
        builder.AppendLine($"Источник: {SafeSourceLabel(entry.SourceType)}.");
        if (!text.IsReviewed || !IsVerified(entry.VerificationStatus))
        {
            builder.AppendLine("Черновик / непроверено: точное значение необходимо сверить с документацией установленной модели.");
        }
        AppendSection(builder, "Возможные причины", text.PossibleCauses);
        AppendSection(builder, "Что проверить", text.CheckSteps);
        AppendSection(builder, "Что не советовать клиенту", text.DoNotAdvise);
        builder.AppendLine();
        builder.AppendLine("Рекомендованное действие:");
        builder.AppendLine(text.RecommendedAction);
    }

    private static void AppendMissingLocalizationFallback(
        StringBuilder builder,
        EquipmentDiagnosticBotResponse response)
    {
        builder.AppendLine(
            $"Техническое описание пока не локализовано. Источник: {SafeSourceLabel(response.SourceCard)}.");
        AppendGenericSafety(builder);
        builder.AppendLine($"Уверенность: {ConfidenceLabel(response.Confidence)}.");
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
            builder.AppendLine($"- {Compact(value, 220)}");
        }
    }

    private static void AppendGenericSafety(StringBuilder builder)
    {
        builder.AppendLine("Безопасность: электрические, компрессорные, инверторные и холодильные проверки выполняет только квалифицированный специалист. Защиты нельзя отключать или обходить.");
    }

    private static ErrorKnowledgeAudience Audience(TelegramUserRole role) =>
        role == TelegramUserRole.Installer
            ? ErrorKnowledgeAudience.Installer
            : ErrorKnowledgeAudience.Engineer;

    private static string SafeSourceLabel(EquipmentDiagnosticBotSourceCard? source) =>
        source?.SourceType switch
        {
            "ManufacturerDocumentation" or "ServiceManual" => "документация производителя",
            "CrossCheckedManuals" => "сверенные руководства производителя",
            "FieldObservation" => "полевое наблюдение, требующее проверки",
            "SeededEngineeringKnowledge" => "встроенный черновой каталог",
            _ => "внутренний диагностический каталог"
        };

    private static string SafeSourceLabel(string sourceType) =>
        sourceType switch
        {
            "Manual" or "ManufacturerDocumentation" or "ServiceManual" =>
                "руководство производителя",
            "CrossCheckedManuals" => "сверенные руководства производителя",
            "FieldObservation" => "полевое наблюдение, требующее проверки",
            "SeededEngineeringKnowledge" => "встроенный черновой каталог",
            _ => "внутренний диагностический каталог"
        };

    private static bool IsVerified(string verificationStatus) =>
        verificationStatus is "ManualVerified" or "Verified" or "Reviewed";

    private static string ConfidenceLabel(string confidence) =>
        confidence switch
        {
            "High" or "ManualVerified" => "Высокая",
            "Medium" => "Средняя",
            "Low" => "Низкая",
            _ => "Черновик / непроверено"
        };

    private static string ConfidenceLabel(DiagnosticConfidence confidence) =>
        confidence switch
        {
            DiagnosticConfidence.Low => "Низкая",
            DiagnosticConfidence.Medium => "Средняя",
            DiagnosticConfidence.High or DiagnosticConfidence.ManualVerified => "Высокая",
            _ => "Черновик / непроверено"
        };

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

    private static string RoleName(TelegramUserRole role) =>
        TelegramUserRolePolicy.DisplayName(role);

    private static string Compact(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3).TrimEnd(), "...");

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
