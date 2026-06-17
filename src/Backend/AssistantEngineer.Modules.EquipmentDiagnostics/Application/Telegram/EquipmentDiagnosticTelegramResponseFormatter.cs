using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramResponseFormatter
{
    public string Format(EquipmentDiagnosticBotResponse response, int maxLength)
    {
        var builder = new StringBuilder();
        builder.AppendLine(response.Title);
        builder.AppendLine(response.Message);
        AppendSafety(builder, response);

        switch (response.Status)
        {
            case EquipmentDiagnosticBotResponseStatus.Answer:
                AppendAnswer(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ClarificationRequired:
                AppendClarification(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ReferenceOnly:
                builder.AppendLine("Reference-only indication; this is not a confirmed fault diagnosis.");
                AppendNextSteps(builder, response.OperatorNextSteps);
                break;
            case EquipmentDiagnosticBotResponseStatus.NotFound:
                builder.AppendLine("Verify the manufacturer, equipment family, display context, and exact service manual.");
                AppendNextSteps(builder, response.OperatorNextSteps);
                break;
            case EquipmentDiagnosticBotResponseStatus.Unsupported:
            case EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope:
                builder.AppendLine("This request is outside the supported deterministic diagnostic flow.");
                AppendNextSteps(builder, response.OperatorNextSteps);
                break;
        }

        return Truncate(builder.ToString().Trim(), maxLength);
    }

    public string FormatHelp(int maxLength) =>
        Truncate(
            "Equipment diagnostics\n" +
            "Send a manufacturer and displayed code, for example: Gree H5.\n" +
            "Optional context examples: Gree C5 outdoor; Gree C5 indoor; /diagnose Gree H5.\n" +
            "Guidance is deterministic and must be verified against the exact installed equipment and service manual.",
            maxLength);

    public string FormatHelp(
        TelegramUserRole role,
        bool hasPhoneNumber,
        int maxLength)
    {
        if (role == TelegramUserRole.Consumer)
        {
            return Truncate(
                "Equipment diagnostics\n" +
                "Send the error code, for example: Gree H5.\n" +
                "You will receive a simple safety-first explanation to share with service.\n" +
                PhonePrompt(hasPhoneNumber),
                maxLength);
        }

        var adminLine = role is TelegramUserRole.Owner or TelegramUserRole.Admin
            ? "\nAdmin: /admin users, /admin allow <chatId>, /admin block <chatId>, /admin role <chatId> <role>."
            : string.Empty;

        return Truncate(
            "Equipment diagnostics\n" +
            "Send a manufacturer and displayed code, for example: Gree H5.\n" +
            "Optional context examples: Gree C5 outdoor; Gree C5 indoor; /diagnose Gree H5.\n" +
            "Technical guidance is deterministic and must be verified against the exact installed equipment and service manual." +
            adminLine,
            maxLength);
    }

    public string FormatConsumer(
        EquipmentDiagnosticBotResponse response,
        bool hasPhoneNumber,
        int maxLength)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"{response.NormalizedManufacturer} {response.NormalizedCode}");
        builder.AppendLine(response.Message);

        if (response.AnswerCard is not null)
        {
            builder.AppendLine(response.AnswerCard.Summary);
        }

        switch (response.Status)
        {
            case EquipmentDiagnosticBotResponseStatus.ClarificationRequired:
                builder.AppendLine(response.ClarificationQuestion?.Prompt ?? "More context is needed before a safer explanation.");
                break;
            case EquipmentDiagnosticBotResponseStatus.NotFound:
                builder.AppendLine("The code was not found in the current checked catalog. Please verify the exact brand and code.");
                break;
            case EquipmentDiagnosticBotResponseStatus.ReferenceOnly:
                builder.AppendLine("This can be only an indication, not a confirmed fault diagnosis.");
                break;
            case EquipmentDiagnosticBotResponseStatus.Unsupported:
            case EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope:
                builder.AppendLine("This request is outside the safe public diagnostic flow.");
                break;
        }

        builder.AppendLine("Safe next steps:");
        builder.AppendLine("- Do not open or disassemble the equipment.");
        builder.AppendLine("- If it is safe, turn the unit off from the remote/control panel and contact service.");
        builder.AppendLine("- Share this code and the equipment model with a qualified specialist.");
        builder.AppendLine(PhonePrompt(hasPhoneNumber));

        return Truncate(builder.ToString().Trim(), maxLength);
    }

    public string FormatMe(
        TelegramUserSnapshot? user,
        int maxLength)
    {
        if (user is null)
        {
            return Truncate("Profile is not available yet.", maxLength);
        }

        return Truncate(
            "Your Telegram access\n" +
            $"Role: {user.Role}\n" +
            $"Enabled: {FormatBool(user.IsEnabled)}\n" +
            $"Blocked: {FormatBool(user.IsBlocked)}\n" +
            $"Phone saved: {FormatBool(user.HasPhoneNumber)}",
            maxLength);
    }

    public string FormatAdminHelp(int maxLength) =>
        Truncate(
            "Admin commands\n" +
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
            return "No Telegram users yet.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Telegram users");
        foreach (var user in users)
        {
            builder.AppendLine(
                $"{user.TelegramChatId}: {user.Role}; enabled={FormatBool(user.IsEnabled)}; blocked={FormatBool(user.IsBlocked)}; phone={FormatBool(user.HasPhoneNumber)}");
        }

        return Truncate(builder.ToString().Trim(), maxLength);
    }

    public string FormatPhoneSaved(int maxLength) =>
        Truncate("Thanks, the phone number is saved. Now send an error code, for example: Gree H5.", maxLength);

    public string FormatValidation(IReadOnlyList<string> errors, int maxLength) =>
        Truncate($"Request not accepted. {string.Join(" ", errors)} Use /help for examples.", maxLength);

    public string FormatUnsupported(int maxLength) =>
        Truncate(
            "Unsupported command or controller model name. Send /help or provide a manufacturer and displayed diagnostic code.",
            maxLength);

    public string FormatIdentity(EquipmentDiagnosticTelegramUpdate update, int maxLength) =>
        Truncate(
            $"Telegram access identity\n" +
            $"chatId: {update.ChatId}\n" +
            $"userId: {update.UserId?.ToString() ?? "not available"}\n" +
            $"username: {update.Username ?? "not available"}\n" +
            "Add chatId to AllowedChatIds in environment configuration, then disable chat ID discovery.",
            maxLength);

    private static void AppendAnswer(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        if (response.VerificationRequired)
        {
            builder.AppendLine("Verification required before final conclusion.");
        }

        builder.AppendLine($"Confidence: {response.Confidence}.");
        if (response.SourceCard is not null)
        {
            builder.AppendLine($"Source: {response.SourceCard.SourceType} / {response.SourceCard.EvidenceLevel}.");
        }

        AppendNextSteps(builder, response.OperatorNextSteps);
    }

    private static void AppendClarification(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        if (response.ClarificationQuestion is null)
        {
            return;
        }

        builder.AppendLine(response.ClarificationQuestion.Prompt);
        foreach (var option in response.ClarificationQuestion.Options)
        {
            builder.AppendLine($"- {option.Label}: reply with {option.EquipmentSide} context.");
        }
    }

    private static void AppendNextSteps(StringBuilder builder, IReadOnlyList<string> steps)
    {
        if (steps.Count == 0)
        {
            return;
        }

        builder.AppendLine("Next steps:");
        foreach (var step in steps.Take(2))
        {
            builder.AppendLine($"- {Compact(step, 140)}");
        }
    }

    private static void AppendSafety(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        builder.AppendLine($"Safety: {response.SafetyCard.Boundary}");
        foreach (var note in response.SafetyCard.Notes.Take(1))
        {
            builder.AppendLine($"- {Compact(note, 140)}");
        }
    }

    private static string Compact(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3).TrimEnd(), "...");

    private static string PhonePrompt(bool hasPhoneNumber) =>
        hasPhoneNumber
            ? "Your contact phone is saved."
            : "Optional: share your Telegram contact if service should be able to call you back.";

    private static string FormatBool(bool value) => value ? "yes" : "no";

    private static string Truncate(string text, int maxLength)
    {
        var effectiveMax = Math.Max(80, maxLength);
        if (text.Length <= effectiveMax)
        {
            return text;
        }

        const string suffix = "\n[Response shortened; use the deterministic bot API for full details.]";
        return string.Concat(text.AsSpan(0, effectiveMax - suffix.Length).TrimEnd(), suffix);
    }
}
