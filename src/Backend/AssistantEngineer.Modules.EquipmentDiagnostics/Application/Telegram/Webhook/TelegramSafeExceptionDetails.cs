using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public static partial class TelegramSafeExceptionDetails
{
    private const string WithheldMessage = "message withheld by logging policy";

    private static readonly string[] SafeMessagePrefixes =
    [
        "No embedded error knowledge JSON resources were found",
        "Error knowledge validation failed",
        "Embedded resource",
        "Error knowledge directory was not found",
        "Knowledge file",
        "Telegram getUpdates returned an unsuccessful API response",
        "Telegram API base URL is invalid",
        "Telegram bot token is missing",
        "database unavailable"
    ];

    public static string Message(Exception exception)
    {
        var message = exception.Message.Trim();
        if (!SafeMessagePrefixes.Any(prefix =>
                message.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return WithheldMessage;
        }

        var safe = TelegramTokenPattern().Replace(message, "[REDACTED]");
        safe = SensitiveAssignmentPattern().Replace(safe, match => $"{match.Groups["name"].Value}=[REDACTED]");
        safe = PhonePattern().Replace(safe, "[REDACTED]");
        safe = RawPlatformIdPattern().Replace(safe, "[REDACTED]");
        safe = CallbackPattern().Replace(safe, "[REDACTED]");
        return safe.Length <= 500 ? safe : string.Concat(safe.AsSpan(0, 497), "...");
    }

    public static string Context(Exception exception)
    {
        var frames = new StackTrace(exception, fNeedFileInfo: false)
            .GetFrames()
            .Select(frame => frame.GetMethod())
            .Where(method => method is not null)
            .Take(6)
            .Select(method =>
                $"{method!.DeclaringType?.FullName ?? "unknown"}.{method.Name}")
            .ToArray();
        return frames.Length == 0 ? "unavailable" : string.Join(" <- ", frames);
    }

    [GeneratedRegex(@"\b\d{8,10}:[A-Za-z0-9_-]{30,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex TelegramTokenPattern();

    [GeneratedRegex(@"(?i)(?<name>BotToken|WebhookSecret|token|secret|Password|Pwd)=([^&;\s]+)", RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveAssignmentPattern();

    [GeneratedRegex(@"\+?\d[\d ()-]{6,}\d", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"(?<!\d)(?:-100)?\d{9,15}(?!\d)", RegexOptions.CultureInvariant)]
    private static partial Regex RawPlatformIdPattern();

    [GeneratedRegex(@"\b(?:sr|sq|au):[a-z0-9:.-]+\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CallbackPattern();
}
