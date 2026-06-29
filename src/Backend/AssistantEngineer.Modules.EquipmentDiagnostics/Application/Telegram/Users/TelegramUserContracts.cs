namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public enum TelegramUserRole
{
    Owner,
    Admin,
    Engineer,
    Installer,
    Consumer
}

public enum TelegramUserPhoneNumberSource
{
    TelegramContact,
    Manual
}

public sealed class TelegramUserEntity
{
    public long Id { get; set; }
    public long TelegramChatId { get; set; }
    public long? TelegramUserId { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public TelegramUserRole Role { get; set; } = TelegramUserRole.Consumer;
    public bool IsEnabled { get; set; } = true;
    public bool IsBlocked { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberVerified { get; set; }
    public TelegramUserPhoneNumberSource? PhoneNumberSource { get; set; }
    public DateTimeOffset? PhoneNumberSharedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public DateTimeOffset? LastAccessDeniedAt { get; set; }
}

public sealed record TelegramUserSnapshot(
    long Id,
    long TelegramChatId,
    long? TelegramUserId,
    string? Username,
    string? FirstName,
    string? LastName,
    TelegramUserRole Role,
    bool IsEnabled,
    bool IsBlocked,
    bool PhoneNumberVerified,
    bool HasPhoneNumber,
    TelegramUserPhoneNumberSource? PhoneNumberSource,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt,
    DateTimeOffset? LastAccessDeniedAt);

public sealed record TelegramUserAccessResult(
    bool IsAllowed,
    TelegramUserSnapshot? User,
    TelegramUserRole Role,
    string? DenialReason = null,
    bool HasLibraryAccessGrant = false)
{
    public bool IsConsumer => Role == TelegramUserRole.Consumer;
    public bool CanUseAdminCommands => TelegramUserRolePolicy.CanManageTelegramUsers(Role);
    public bool UsesTechnicalResponse => TelegramUserRolePolicy.CanViewTechnicalDiagnostics(Role);
    public bool CanUseServiceQueue => TelegramUserRolePolicy.CanUseServiceQueue(Role);
    public bool CanAccessDiagnosticManual => TelegramUserRolePolicy.CanAccessDiagnosticManual(Role);
    public bool CanAccessLibrary =>
        TelegramUserRolePolicy.CanAccessTelegramLibrary(Role, HasLibraryAccessGrant) &&
        User?.IsEnabled == true &&
        User.IsBlocked == false;
}

public sealed record TelegramUserCommandResult(
    bool Succeeded,
    string Message);

public sealed record TelegramUserPrivateContact(
    long TelegramUserDatabaseId,
    long PrivateChatId,
    string PhoneNumber);

public interface ITelegramUserStore
{
    Task<TelegramUserSnapshot> EnsureBootstrapOwnerAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default);

    Task<TelegramUserSnapshot> GetOrCreateConsumerAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default);

    Task<TelegramUserSnapshot?> GetByChatIdAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task<TelegramUserSnapshot?> GetByIdAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default);

    Task<TelegramUserSnapshot?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<TelegramUserSnapshot?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<TelegramUserPrivateContact?> GetPrivateContactAsync(
        long telegramUserDatabaseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelegramUserSnapshot>> ListUsersAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task MarkAccessDeniedAsync(
        long chatId,
        CancellationToken cancellationToken = default);

    Task<TelegramUserCommandResult> AllowAsync(
        long chatId,
        TelegramUserRole role,
        CancellationToken cancellationToken = default);

    Task<TelegramUserCommandResult> SetRoleAsync(
        long chatId,
        TelegramUserRole role,
        CancellationToken cancellationToken = default);

    Task<TelegramUserCommandResult> SetEnabledAsync(
        long chatId,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    Task<TelegramUserCommandResult> SetBlockedAsync(
        long chatId,
        bool isBlocked,
        CancellationToken cancellationToken = default);

    Task<TelegramUserCommandResult> SavePhoneAsync(
        long chatId,
        string phoneNumber,
        bool verified,
        TelegramUserPhoneNumberSource source,
        DateTimeOffset sharedAt,
        CancellationToken cancellationToken = default);
}
