using System.Text.Json.Serialization;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed record EquipmentDiagnosticTelegramWebhookOptions
{
    public bool IsEnabled { get; init; }
    public string? WebhookSecret { get; init; }
    public string? BotToken { get; init; }
    public IReadOnlyCollection<long> AllowedChatIds { get; init; } = [];
    public IReadOnlyCollection<string> AllowedUsernames { get; init; } = [];
    public int MaxMessageLength { get; init; } = 500;
    public string? DefaultManufacturer { get; init; } = "Gree";
    public string? PreferredLanguage { get; init; }
    public bool EnableFreeTextParsing { get; init; }
    public bool RequireExplicitManufacturer { get; init; }
    public int SendMessageTimeoutSeconds { get; init; } = 10;
    public string TelegramApiBaseUrl { get; init; } = "https://api.telegram.org";
    public bool DropPendingUpdatesOnSetWebhook { get; init; }
    public IReadOnlyCollection<string> AllowedUpdates { get; init; } = ["message"];
}

public sealed record TelegramWebhookUpdateDto(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramWebhookMessageDto? Message);

public sealed record TelegramWebhookMessageDto(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("chat")] TelegramWebhookChatDto? Chat,
    [property: JsonPropertyName("from")] TelegramWebhookUserDto? From,
    [property: JsonPropertyName("date")] long? Date);

public sealed record TelegramWebhookChatDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string? Username);

public sealed record TelegramWebhookUserDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string? Username);

public enum EquipmentDiagnosticTelegramWebhookStatus
{
    Processed,
    Ignored,
    Rejected,
    OutboundFailed,
    InvalidUpdate,
    Unauthorized,
    Disabled
}

public sealed record EquipmentDiagnosticTelegramWebhookResult(
    EquipmentDiagnosticTelegramWebhookStatus Status,
    string Message);

public sealed record EquipmentDiagnosticTelegramOutboundResult(
    bool Succeeded,
    string Message);
