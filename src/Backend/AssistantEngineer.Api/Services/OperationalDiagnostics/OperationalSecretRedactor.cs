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
}
