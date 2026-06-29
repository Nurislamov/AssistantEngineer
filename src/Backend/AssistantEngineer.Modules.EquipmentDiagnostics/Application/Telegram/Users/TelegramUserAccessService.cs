using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Manuals;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public interface ITelegramUserAccessService
{
    Task<TelegramUserAccessResult> ResolveAccessAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default);
}

public sealed class TelegramUserAccessService : ITelegramUserAccessService
{
    private readonly ITelegramUserStore _store;
    private readonly EquipmentDiagnosticTelegramOptions _options;
    private readonly ITelegramLibraryAccessStore? _libraryAccessStore;

    public TelegramUserAccessService(
        ITelegramUserStore store,
        EquipmentDiagnosticTelegramOptions options,
        ITelegramLibraryAccessStore? libraryAccessStore = null)
    {
        _store = store;
        _options = options;
        _libraryAccessStore = libraryAccessStore;
    }

    public async Task<TelegramUserAccessResult> ResolveAccessAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (IsDeniedByEmergencyConfig(update))
        {
            await _store.MarkAccessDeniedAsync(update.ChatId, cancellationToken);
            return Denied(TelegramUserRole.Consumer, "Denied by emergency allow/deny configuration.");
        }

        if (ResolveBootstrapOwnerChatId() == update.ChatId)
        {
            var owner = await _store.EnsureBootstrapOwnerAsync(update, cancellationToken);
            return await AllowedAsync(owner, cancellationToken);
        }

        var user = await _store.GetOrCreateConsumerAsync(update, cancellationToken);
        if (user.IsBlocked)
        {
            await _store.MarkAccessDeniedAsync(update.ChatId, cancellationToken);
            return Denied(user.Role, "Telegram user is blocked.", user);
        }

        if (!user.IsEnabled)
        {
            await _store.MarkAccessDeniedAsync(update.ChatId, cancellationToken);
            return Denied(user.Role, "Telegram user is disabled.", user);
        }

        return await AllowedAsync(user, cancellationToken);
    }

    private long? ResolveBootstrapOwnerChatId() =>
        _options.BootstrapOwnerChatId ??
        (_options.AllowedChatIds.Count > 0 ? _options.AllowedChatIds.First() : null);

    private bool IsDeniedByEmergencyConfig(EquipmentDiagnosticTelegramUpdate update) =>
        _options.DeniedChatIds.Contains(update.ChatId) ||
        update.Username is not null &&
        _options.DeniedUsernames.Contains(update.Username, StringComparer.OrdinalIgnoreCase);

    private async Task<TelegramUserAccessResult> AllowedAsync(
        TelegramUserSnapshot user,
        CancellationToken cancellationToken)
    {
        var needsLibraryGrantLookup = user.Role is TelegramUserRole.Admin or TelegramUserRole.Engineer or TelegramUserRole.Installer;
        var hasLibraryGrant = needsLibraryGrantLookup &&
            _libraryAccessStore is not null &&
            await _libraryAccessStore.HasActiveGrantAsync(user.Id, cancellationToken);
        return new TelegramUserAccessResult(true, user, user.Role, HasLibraryAccessGrant: hasLibraryGrant);
    }

    private static TelegramUserAccessResult Denied(
        TelegramUserRole role,
        string reason,
        TelegramUserSnapshot? user = null) =>
        new(false, user, role, reason);
}
