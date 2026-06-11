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
    Unsupported
}

public sealed record EquipmentDiagnosticTelegramOptions
{
    public bool IsEnabled { get; init; }
    public IReadOnlyCollection<long> AllowedChatIds { get; init; } = [];
    public IReadOnlyCollection<string> AllowedUsernames { get; init; } = [];
    public int MaxMessageLength { get; init; } = 500;
    public string? DefaultManufacturer { get; init; } = "Gree";
    public string? PreferredLanguage { get; init; }
    public bool EnableFreeTextParsing { get; init; }
    public bool RequireExplicitManufacturer { get; init; }
}

public sealed record EquipmentDiagnosticTelegramUpdate(
    long UpdateId,
    long ChatId,
    string? Username,
    string? Text,
    long? MessageId = null,
    DateTimeOffset? ReceivedAt = null);

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
    IReadOnlyList<string>? InternalDecisionTrace = null);
