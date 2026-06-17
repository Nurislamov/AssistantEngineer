using System.Text.Json.Serialization;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed record EquipmentDiagnosticTelegramWebhookOptions
{
    public bool IsEnabled { get; init; }
    public EquipmentDiagnosticTelegramInboundMode InboundMode { get; init; } = EquipmentDiagnosticTelegramInboundMode.Webhook;
    public bool DeleteWebhookOnStartup { get; init; }
    public string? WebhookSecret { get; init; }
    public string? BotToken { get; init; }
    public IReadOnlyCollection<long> AllowedChatIds { get; init; } = [];
    public IReadOnlyCollection<string> AllowedUsernames { get; init; } = [];
    public IReadOnlyCollection<long> DeniedChatIds { get; init; } = [];
    public IReadOnlyCollection<string> DeniedUsernames { get; init; } = [];
    public long? BootstrapOwnerChatId { get; init; }
    public bool EnableChatIdDiscovery { get; init; }
    public int MaxMessageLength { get; init; } = 500;
    public string? DefaultManufacturer { get; init; } = "Gree";
    public string? PreferredLanguage { get; init; }
    public bool EnableFreeTextParsing { get; init; }
    public bool RequireExplicitManufacturer { get; init; }
    public int SendMessageTimeoutSeconds { get; init; } = 10;
    public string TelegramApiBaseUrl { get; init; } = "https://api.telegram.org";
    public bool DropPendingUpdatesOnSetWebhook { get; init; }
    public IReadOnlyCollection<string> AllowedUpdates { get; init; } = ["message"];
    public EquipmentDiagnosticTelegramPollingOptions Polling { get; init; } = new();
    public EquipmentDiagnosticTelegramCommandMenuOptions Commands { get; init; } = new();

    public bool IsPollingDeliveryEnabled() =>
        IsEnabled &&
        !string.IsNullOrWhiteSpace(BotToken) &&
        (InboundMode == EquipmentDiagnosticTelegramInboundMode.Polling || Polling.Enabled);
}

public sealed record EquipmentDiagnosticTelegramCommandMenuOptions
{
    public bool SyncOnStartup { get; init; } = true;
}

public enum EquipmentDiagnosticTelegramInboundMode
{
    Webhook,
    Polling
}

public sealed record EquipmentDiagnosticTelegramPollingOptions
{
    public bool Enabled { get; init; }
    public int TimeoutSeconds { get; init; } = 50;
    public int Limit { get; init; } = 25;
    public int DelayAfterErrorSeconds { get; init; } = 10;
    public string OffsetStoreFilePath { get; init; } = "artifacts/operations/equipment-diagnostics-telegram-offset.txt";
    public string ProcessedMessageStoreFilePath { get; init; } = "artifacts/operations/equipment-diagnostics-telegram-processed-messages.txt";
    public int ProcessedMessageStoreMaxEntries { get; init; } = 5000;
}

public sealed record TelegramWebhookUpdateDto(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TelegramWebhookMessageDto? Message);

public sealed record TelegramWebhookMessageDto(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("chat")] TelegramWebhookChatDto? Chat,
    [property: JsonPropertyName("from")] TelegramWebhookUserDto? From,
    [property: JsonPropertyName("date")] long? Date,
    [property: JsonPropertyName("contact")] TelegramWebhookContactDto? Contact = null);

public sealed record TelegramWebhookChatDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("type")] string? Type = null);

public sealed record TelegramWebhookUserDto(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("first_name")] string? FirstName = null,
    [property: JsonPropertyName("last_name")] string? LastName = null);

public sealed record TelegramWebhookContactDto(
    [property: JsonPropertyName("phone_number")] string PhoneNumber,
    [property: JsonPropertyName("user_id")] long? UserId);

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

public sealed record EquipmentDiagnosticTelegramDeleteWebhookResult(
    bool Succeeded,
    string Message);

public sealed record EquipmentDiagnosticTelegramSetCommandsResult(
    bool Succeeded,
    string Message);

public sealed record EquipmentDiagnosticTelegramBotCommand(
    string Command,
    string Description);
