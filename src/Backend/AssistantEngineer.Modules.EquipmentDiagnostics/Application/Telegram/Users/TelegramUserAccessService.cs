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

    public TelegramUserAccessService(
        ITelegramUserStore store,
        EquipmentDiagnosticTelegramOptions options)
    {
        _store = store;
        _options = options;
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
            return Allowed(owner);
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

        return Allowed(user);
    }

    private long? ResolveBootstrapOwnerChatId() =>
        _options.BootstrapOwnerChatId ??
        (_options.AllowedChatIds.Count > 0 ? _options.AllowedChatIds.First() : null);

    private bool IsDeniedByEmergencyConfig(EquipmentDiagnosticTelegramUpdate update) =>
        _options.DeniedChatIds.Contains(update.ChatId) ||
        update.Username is not null &&
        _options.DeniedUsernames.Contains(update.Username, StringComparer.OrdinalIgnoreCase);

    private static TelegramUserAccessResult Allowed(TelegramUserSnapshot user) =>
        new(true, user, user.Role);

    private static TelegramUserAccessResult Denied(
        TelegramUserRole role,
        string reason,
        TelegramUserSnapshot? user = null) =>
        new(false, user, role, reason);
}
