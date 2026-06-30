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
    public string? DisplayTimeZone { get; init; } = "Asia/Tashkent";
    public TelegramServiceRequestOptions ServiceRequests { get; init; } = new();
    public TelegramManualLibraryOptions ManualLibrary { get; init; } = new();
    public TelegramOperatorInboxOptions OperatorInbox { get; init; } = new();
    public int SendMessageTimeoutSeconds { get; init; } = 10;
    public string TelegramApiBaseUrl { get; init; } = "https://api.telegram.org";
    public bool DropPendingUpdatesOnSetWebhook { get; init; }
    public IReadOnlyCollection<string> AllowedUpdates { get; init; } = ["message", "callback_query"];
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
    [property: JsonPropertyName("message")] TelegramWebhookMessageDto? Message,
    [property: JsonPropertyName("callback_query")] TelegramWebhookCallbackQueryDto? CallbackQuery = null);

public sealed record TelegramWebhookMessageDto(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("chat")] TelegramWebhookChatDto? Chat,
    [property: JsonPropertyName("from")] TelegramWebhookUserDto? From,
    [property: JsonPropertyName("date")] long? Date,
    [property: JsonPropertyName("contact")] TelegramWebhookContactDto? Contact = null,
    [property: JsonPropertyName("caption")] string? Caption = null,
    [property: JsonPropertyName("document")] TelegramWebhookDocumentDto? Document = null,
    [property: JsonPropertyName("photo")] IReadOnlyList<TelegramWebhookPhotoSizeDto>? Photo = null,
    [property: JsonPropertyName("video")] TelegramWebhookVideoDto? Video = null,
    [property: JsonPropertyName("voice")] TelegramWebhookVoiceDto? Voice = null,
    [property: JsonPropertyName("video_note")] TelegramWebhookVideoNoteDto? VideoNote = null,
    [property: JsonPropertyName("audio")] TelegramWebhookAudioDto? Audio = null,
    [property: JsonPropertyName("location")] TelegramWebhookLocationDto? Location = null,
    [property: JsonPropertyName("animation")] TelegramWebhookAnimationDto? Animation = null,
    [property: JsonPropertyName("reply_to_message")] TelegramWebhookMessageDto? ReplyToMessage = null);

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

public sealed record TelegramWebhookDocumentDto(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string? FileUniqueId = null,
    [property: JsonPropertyName("file_name")] string? FileName = null,
    [property: JsonPropertyName("mime_type")] string? MimeType = null,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramWebhookPhotoSizeDto(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string? FileUniqueId = null,
    [property: JsonPropertyName("width")] int? Width = null,
    [property: JsonPropertyName("height")] int? Height = null,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramWebhookVideoDto(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string? FileUniqueId = null,
    [property: JsonPropertyName("file_name")] string? FileName = null,
    [property: JsonPropertyName("mime_type")] string? MimeType = null,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramWebhookVoiceDto(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string? FileUniqueId = null,
    [property: JsonPropertyName("mime_type")] string? MimeType = null,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramWebhookVideoNoteDto(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string? FileUniqueId = null,
    [property: JsonPropertyName("duration")] int? Duration = null,
    [property: JsonPropertyName("length")] int? Length = null,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramWebhookAudioDto(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string? FileUniqueId = null,
    [property: JsonPropertyName("duration")] int? Duration = null,
    [property: JsonPropertyName("performer")] string? Performer = null,
    [property: JsonPropertyName("title")] string? Title = null,
    [property: JsonPropertyName("file_name")] string? FileName = null,
    [property: JsonPropertyName("mime_type")] string? MimeType = null,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramWebhookLocationDto(
    [property: JsonPropertyName("latitude")] double Latitude,
    [property: JsonPropertyName("longitude")] double Longitude);

public sealed record TelegramWebhookAnimationDto(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_unique_id")] string? FileUniqueId = null,
    [property: JsonPropertyName("file_name")] string? FileName = null,
    [property: JsonPropertyName("mime_type")] string? MimeType = null,
    [property: JsonPropertyName("duration")] int? Duration = null,
    [property: JsonPropertyName("width")] int? Width = null,
    [property: JsonPropertyName("height")] int? Height = null,
    [property: JsonPropertyName("file_size")] long? FileSize = null);

public sealed record TelegramWebhookCallbackQueryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("from")] TelegramWebhookUserDto From,
    [property: JsonPropertyName("message")] TelegramWebhookMessageDto? Message,
    [property: JsonPropertyName("data")] string? Data);

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
    string Message,
    long? MessageId = null);

public sealed record EquipmentDiagnosticTelegramDeleteWebhookResult(
    bool Succeeded,
    string Message);

public sealed record EquipmentDiagnosticTelegramSetCommandsResult(
    bool Succeeded,
    string Message);

public sealed record EquipmentDiagnosticTelegramBotCommand(
    string Command,
    string Description);
