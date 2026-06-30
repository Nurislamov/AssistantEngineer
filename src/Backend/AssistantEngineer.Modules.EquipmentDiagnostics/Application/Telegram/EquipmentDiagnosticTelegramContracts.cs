using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public enum EquipmentDiagnosticTelegramResponseKind
{
    Reply,
    Ignored,
    ValidationError,
    Unsupported
}

public enum EquipmentDiagnosticTelegramCommand
{
    Diagnose,
    Start,
    Help,
    History,
    Last,
    Request,
    Requests,
    Identity,
    Unsupported
}

public sealed record EquipmentDiagnosticTelegramOptions
{
    public bool IsEnabled { get; init; }
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
}

public sealed record TelegramServiceRequestOptions
{
    public long? NotificationChatId { get; init; }
    public bool NotifyOnCreate { get; init; } = true;
}

public sealed record TelegramManualLibraryOptions
{
    public bool Enabled { get; init; } = true;
    public string FileBindingsPath { get; init; } = "artifacts/operations/equipment-diagnostics-manual-bindings.json";
    public int MaxFilesPerRequest { get; init; } = 5;
    public IReadOnlyCollection<string> AllowedExtensions { get; init; } = [".pdf", ".doc", ".docx", ".xls", ".xlsx"];
    public IReadOnlyCollection<long> TrustedStorageChatIds { get; init; } = [];
}

public sealed record TelegramOperatorInboxOptions
{
    public bool Enabled { get; init; }
    public long? ChatId { get; init; }
    public bool LogDiagnostics { get; init; }
}

public sealed record EquipmentDiagnosticTelegramUpdate(
    long UpdateId,
    long ChatId,
    string? Username,
    string? Text,
    long? MessageId = null,
    DateTimeOffset? ReceivedAt = null,
    long? UserId = null,
    string? FirstName = null,
    string? LastName = null,
    string? ContactPhoneNumber = null,
    long? ContactUserId = null,
    string? ChatType = null,
    string? CallbackQueryId = null,
    string? CallbackData = null,
    string? DocumentFileId = null,
    string? DocumentFileName = null,
    string? DocumentMimeType = null,
    long? DocumentFileSize = null,
    string? ReplyToDocumentFileId = null,
    string? ReplyToDocumentFileName = null,
    string? ReplyToDocumentMimeType = null,
    long? ReplyToDocumentFileSize = null,
    string? DocumentFileUniqueId = null,
    string? ReplyToDocumentFileUniqueId = null,
    long? ReplyToMessageId = null,
    bool HasPhoto = false,
    bool HasVideo = false,
    bool HasVoice = false,
    bool HasVideoNote = false,
    bool HasAudio = false,
    bool HasLocation = false,
    bool HasAnimation = false);

public sealed record EquipmentDiagnosticTelegramParseResult(
    EquipmentDiagnosticTelegramCommand Command,
    EquipmentDiagnosticBotRequest? DiagnosticRequest,
    IReadOnlyList<string> Errors);

public sealed record EquipmentDiagnosticTelegramResponse(
    long ChatId,
    string Text,
    EquipmentDiagnosticTelegramResponseKind ResponseKind,
    string? ParseMode,
    bool DisableWebPagePreview,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string>? InternalDecisionTrace = null,
    IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage>? Messages = null,
    string? CallbackAnswerText = null,
    bool SuppressOutbound = false)
{
    public IReadOnlyList<EquipmentDiagnosticTelegramOutboundMessage> OutboundMessages =>
        SuppressOutbound
            ? []
            : Messages is { Count: > 0 }
            ? Messages
            :
            [
                new EquipmentDiagnosticTelegramOutboundMessage(
                    Text,
                    ParseMode,
                    DisableWebPagePreview)
            ];
}

public sealed record EquipmentDiagnosticTelegramOutboundMessage(
    string Text,
    string? ParseMode = null,
    bool DisableWebPagePreview = true,
    EquipmentDiagnosticTelegramReplyMarkup? ReplyMarkup = null,
    string? DocumentFileId = null,
    string? DocumentFileName = null,
    bool ProtectContent = false,
    long? EditMessageId = null);

public sealed record EquipmentDiagnosticTelegramReplyMarkup(
    IReadOnlyList<IReadOnlyList<EquipmentDiagnosticTelegramKeyboardButton>>? Keyboard = null,
    bool? ResizeKeyboard = null,
    bool? OneTimeKeyboard = null,
    bool? RemoveKeyboard = null,
    IReadOnlyList<IReadOnlyList<EquipmentDiagnosticTelegramInlineKeyboardButton>>? InlineKeyboard = null);

public sealed record EquipmentDiagnosticTelegramKeyboardButton(
    string Text,
    bool RequestContact = false);

public sealed record EquipmentDiagnosticTelegramInlineKeyboardButton(
    string Text,
    string CallbackData);
