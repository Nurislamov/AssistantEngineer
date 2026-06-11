using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed partial class EquipmentDiagnosticTelegramWebhookSecurityPolicy
{
    public EquipmentDiagnosticTelegramWebhookResult Validate(
        EquipmentDiagnosticTelegramWebhookOptions options,
        string? suppliedSecret)
    {
        if (!options.IsEnabled)
        {
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Disabled, "Telegram webhook transport is disabled.");
        }

        if (!IsValidSecret(options.WebhookSecret))
        {
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Rejected, "Telegram webhook secret configuration is invalid.");
        }

        if (string.IsNullOrEmpty(suppliedSecret) || !ConstantTimeEquals(options.WebhookSecret!, suppliedSecret))
        {
            return Result(EquipmentDiagnosticTelegramWebhookStatus.Unauthorized, "Telegram webhook authentication failed.");
        }

        return Result(EquipmentDiagnosticTelegramWebhookStatus.Processed, "Telegram webhook authentication accepted.");
    }

    public bool IsValidSecret(string? secret) =>
        secret is { Length: >= 1 and <= 256 } && SecretPattern().IsMatch(secret);

    private static bool ConstantTimeEquals(string configured, string supplied) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(configured),
            Encoding.UTF8.GetBytes(supplied));

    private static EquipmentDiagnosticTelegramWebhookResult Result(
        EquipmentDiagnosticTelegramWebhookStatus status,
        string message) => new(status, message);

    [GeneratedRegex("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SecretPattern();
}
