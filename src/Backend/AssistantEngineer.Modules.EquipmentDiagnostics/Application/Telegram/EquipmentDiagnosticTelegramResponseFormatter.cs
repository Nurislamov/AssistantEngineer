using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramResponseFormatter
{
    public string Format(EquipmentDiagnosticBotResponse response, int maxLength) =>
        Truncate(FormatTechnical(response), maxLength);

    public string FormatTechnical(EquipmentDiagnosticBotResponse response)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Диагностика {response.NormalizedManufacturer} {response.NormalizedCode}");
        builder.AppendLine(response.Message);
        AppendTechnicalSafety(builder, response);

        switch (response.Status)
        {
            case EquipmentDiagnosticBotResponseStatus.Answer:
                AppendTechnicalAnswer(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ClarificationRequired:
                AppendTechnicalClarification(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ReferenceOnly:
                builder.AppendLine("Справочное совпадение: это не подтвержденный диагноз неисправности.");
                AppendTechnicalNextSteps(builder, response.OperatorNextSteps);
                break;
            case EquipmentDiagnosticBotResponseStatus.NotFound:
                builder.AppendLine("Код не найден в текущем проверенном каталоге. Проверьте производителя, семейство оборудования, место отображения кода и точную сервисную документацию.");
                AppendTechnicalNextSteps(builder, response.OperatorNextSteps);
                break;
            case EquipmentDiagnosticBotResponseStatus.Unsupported:
            case EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope:
                builder.AppendLine("Запрос вне поддерживаемого безопасного диагностического сценария.");
                AppendTechnicalNextSteps(builder, response.OperatorNextSteps);
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

        var adminLine = role is TelegramUserRole.Owner or TelegramUserRole.Admin
            ? "\n\nАдмин-команды: /admin_help"
            : string.Empty;

        return Truncate(
            "Диагностика оборудования\n\n" +
            "Напишите производителя и код ошибки, например: Gree H5.\n" +
            "Можно добавить контекст: Gree C5 outdoor; Gree C5 indoor; /diagnose Gree H5.\n" +
            "История: /history, последняя диагностика: /last.\n" +
            "Техническая подсказка требует проверки по точной модели и сервисной документации." +
            adminLine,
            maxLength);
    }

    public string FormatConsumer(
        EquipmentDiagnosticBotResponse response,
        bool hasPhoneNumber,
        int maxLength)
    {
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
            "/admin users\n" +
            "/admin allow <chatId>\n" +
            "/admin block <chatId>\n" +
            "/admin unblock <chatId>\n" +
            "/admin disable <chatId>\n" +
            "/admin enable <chatId>\n" +
            "/admin role <chatId> <Owner|Admin|Engineer|Consumer>",
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

    private static void AppendTechnicalAnswer(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        if (response.VerificationRequired)
        {
            builder.AppendLine("Перед окончательным выводом нужна проверка по месту.");
        }

        builder.AppendLine($"Уверенность: {response.Confidence}.");
        if (response.SourceCard is not null)
        {
            builder.AppendLine($"Источник: {response.SourceCard.SourceType} / {response.SourceCard.EvidenceLevel}.");
        }

        if (response.AnswerCard is not null)
        {
            builder.AppendLine();
            builder.AppendLine("Кратко:");
            builder.AppendLine(response.AnswerCard.Summary);
        }

        AppendTechnicalNextSteps(builder, response.OperatorNextSteps);
    }

    private static void AppendTechnicalClarification(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        if (response.ClarificationQuestion is null)
        {
            return;
        }

        builder.AppendLine(response.ClarificationQuestion.Prompt);
        foreach (var option in response.ClarificationQuestion.Options)
        {
            builder.AppendLine($"- {option.Label}: ответьте с контекстом {option.EquipmentSide}.");
        }
    }

    private static void AppendTechnicalNextSteps(StringBuilder builder, IReadOnlyList<string> steps)
    {
        if (steps.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Следующие шаги:");
        foreach (var step in steps.Take(4))
        {
            builder.AppendLine($"- {Compact(step, 180)}");
        }
    }

    private static void AppendTechnicalSafety(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        builder.AppendLine($"Безопасность: {response.SafetyCard.Boundary}");
        foreach (var note in response.SafetyCard.Notes.Take(2))
        {
            builder.AppendLine($"- {Compact(note, 180)}");
        }
    }

    private static string PhonePrompt(bool hasPhoneNumber) =>
        hasPhoneNumber
            ? "Ваш номер уже сохранен."
            : "Если хотите, оставьте номер телефона:\n" +
              "- можно поделиться номером Telegram\n" +
              "- или ввести другой номер для звонка.";

    private static string RoleName(TelegramUserRole role) =>
        role switch
        {
            TelegramUserRole.Owner => "Владелец",
            TelegramUserRole.Admin => "Администратор",
            TelegramUserRole.Engineer => "Инженер",
            TelegramUserRole.Consumer => "Пользователь",
            _ => role.ToString()
        };

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
