using System.Text.RegularExpressions;

namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public static partial class OperationalSecretRedactor
{
    private const string Redacted = "[REDACTED]";

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var redacted = TelegramTokenPattern().Replace(value, Redacted);
        redacted = SensitiveHeaderPattern().Replace(redacted, match => $"{match.Groups["name"].Value}: {Redacted}");
        redacted = SensitiveAssignmentPattern().Replace(redacted, match => $"{match.Groups["name"].Value}={Redacted}");
        redacted = ConnectionStringPasswordPattern().Replace(redacted, match => $"{match.Groups["name"].Value}={Redacted}");
        redacted = ChatListPattern().Replace(redacted, match => $"{match.Groups["prefix"].Value}{Redacted}");
        redacted = ChatIdPattern().Replace(redacted, match => $"{match.Groups["prefix"].Value}\"{Redacted}\"");
        redacted = TelegramUsernameFieldPattern().Replace(redacted, match => $"{match.Groups["prefix"].Value}\"{Redacted}\"");
        redacted = PhoneNumberFieldPattern().Replace(redacted, match => $"{match.Groups["prefix"].Value}\"{Redacted}\"");
        redacted = TelegramMessageFieldPattern().Replace(redacted, match => $"{match.Groups["prefix"].Value}\"{Redacted}\"");
        return redacted;
    }

    [GeneratedRegex(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex TelegramTokenPattern();

    [GeneratedRegex(@"(?im)(?<name>Authorization|X-Telegram-Bot-Api-Secret-Token)\s*:\s*[^\r\n]+", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveHeaderPattern();

    [GeneratedRegex(@"(?i)(?<name>BotToken|WebhookSecret|token|secret)=([^&\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveAssignmentPattern();

    [GeneratedRegex(@"(?i)(?<name>Password|Pwd)=([^;\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex ConnectionStringPasswordPattern();

    [GeneratedRegex(@"(?im)^(?<prefix>.*\b(?:AllowedChatIds|DeniedChatIds|AllowedUsernames|DeniedUsernames)\b\s*[=:]).*$", RegexOptions.CultureInvariant)]
    private static partial Regex ChatListPattern();

    [GeneratedRegex(@"(?i)(?<prefix>""?chat_id""?\s*[=:]\s*)""?-?\d+""?", RegexOptions.CultureInvariant)]
    private static partial Regex ChatIdPattern();

    [GeneratedRegex(@"(?i)(?<prefix>""?(?:username|from_username|fromUsername)""?\s*[=:]\s*)""(?:\\.|[^""\\])*""", RegexOptions.CultureInvariant)]
    private static partial Regex TelegramUsernameFieldPattern();

    [GeneratedRegex(@"(?i)(?<prefix>""?(?:phone_number|phoneNumber|PhoneNumber)""?\s*[=:]\s*)""?[\+0-9][0-9()\-\s]{5,}""?", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneNumberFieldPattern();

    [GeneratedRegex(@"(?i)(?<prefix>""?(?:text|message_body|messageBody|telegramMessage)""?\s*[=:]\s*)""(?:\\.|[^""\\])*""", RegexOptions.CultureInvariant)]
    private static partial Regex TelegramMessageFieldPattern();
}
