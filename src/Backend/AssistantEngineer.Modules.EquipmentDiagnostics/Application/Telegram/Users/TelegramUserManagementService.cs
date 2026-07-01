using System.Text.Json;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public enum TelegramUserManagementStatus
{
    Success,
    NotFound,
    AccessDenied,
    CannotModifySelf,
    CannotModifyLastOwner,
    AlreadyBlocked,
    AlreadyActive,
    InvalidRole
}

public sealed record TelegramUserManagementResult(
    TelegramUserManagementStatus Status,
    TelegramUserSnapshot? User = null);

public sealed class TelegramUserManagementService
{
    private readonly ITelegramUserStore _userStore;
    private readonly TelegramUserAuditEventService? _auditService;

    public TelegramUserManagementService(
        ITelegramUserStore userStore,
        TelegramUserAuditEventService? auditService = null)
    {
        _userStore = userStore;
        _auditService = auditService;
    }

    public async Task<TelegramUserManagementResult> GetUserDetailsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userStore.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? new(TelegramUserManagementStatus.NotFound)
            : new(TelegramUserManagementStatus.Success, user);
    }

    public async Task<TelegramUserManagementResult> ChangeUserRoleAsync(
        long targetUserId,
        TelegramUserRole newRole,
        long actorUserId,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveMutationAsync(targetUserId, actorUserId, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        if (!Enum.IsDefined(newRole))
        {
            return await DeniedAsync(
                TelegramUserManagementStatus.InvalidRole,
                context.Actor,
                context.Target,
                "role",
                "invalid_role",
                cancellationToken);
        }

        if (context.Target!.Role == TelegramUserRole.Owner &&
            newRole != TelegramUserRole.Owner &&
            await IsLastOwnerAsync(cancellationToken))
        {
            return await DeniedAsync(
                TelegramUserManagementStatus.CannotModifyLastOwner,
                context.Actor,
                context.Target,
                "role",
                "last_owner_protected",
                cancellationToken);
        }

        if (context.Actor!.Id == context.Target!.Id)
        {
            return await DeniedAsync(
                TelegramUserManagementStatus.CannotModifySelf,
                context.Actor,
                context.Target,
                "role",
                "self_action_denied",
                cancellationToken);
        }

        if (context.Target.Role == newRole)
        {
            return new(TelegramUserManagementStatus.Success, context.Target);
        }

        var command = await _userStore.SetRoleAsync(
            context.Target.TelegramChatId,
            newRole,
            cancellationToken);
        if (!command.Succeeded)
        {
            return new(TelegramUserManagementStatus.NotFound);
        }

        var updated = await _userStore.GetByIdAsync(context.Target.Id, cancellationToken);
        await AppendAuditAsync(
            TelegramUserAuditEventType.RoleChanged,
            context.Actor,
            context.Target,
            updated,
            "role",
            cancellationToken);
        return new(TelegramUserManagementStatus.Success, updated);
    }

    public Task<TelegramUserManagementResult> BlockUserAsync(
        long targetUserId,
        long actorUserId,
        CancellationToken cancellationToken = default) =>
        SetBlockedAsync(targetUserId, actorUserId, true, cancellationToken);

    public Task<TelegramUserManagementResult> UnblockUserAsync(
        long targetUserId,
        long actorUserId,
        CancellationToken cancellationToken = default) =>
        SetBlockedAsync(targetUserId, actorUserId, false, cancellationToken);

    private async Task<TelegramUserManagementResult> SetBlockedAsync(
        long targetUserId,
        long actorUserId,
        bool isBlocked,
        CancellationToken cancellationToken)
    {
        var action = isBlocked ? "block" : "unblock";
        var context = await ResolveMutationAsync(targetUserId, actorUserId, cancellationToken);
        if (context.Result is not null)
        {
            return context.Result;
        }

        if (context.Actor!.Id == context.Target!.Id)
        {
            return await DeniedAsync(
                TelegramUserManagementStatus.CannotModifySelf,
                context.Actor,
                context.Target,
                action,
                "self_action_denied",
                cancellationToken);
        }

        if (isBlocked &&
            context.Target.Role == TelegramUserRole.Owner &&
            await IsLastOwnerAsync(cancellationToken))
        {
            return await DeniedAsync(
                TelegramUserManagementStatus.CannotModifyLastOwner,
                context.Actor,
                context.Target,
                action,
                "last_owner_protected",
                cancellationToken);
        }

        if (isBlocked && context.Target.IsBlocked)
        {
            return new(TelegramUserManagementStatus.AlreadyBlocked, context.Target);
        }

        if (!isBlocked && !context.Target.IsBlocked)
        {
            return new(TelegramUserManagementStatus.AlreadyActive, context.Target);
        }

        var command = await _userStore.SetBlockedAsync(
            context.Target.TelegramChatId,
            isBlocked,
            cancellationToken);
        if (!command.Succeeded)
        {
            return new(TelegramUserManagementStatus.NotFound);
        }

        var updated = await _userStore.GetByIdAsync(context.Target.Id, cancellationToken);
        await AppendAuditAsync(
            isBlocked
                ? TelegramUserAuditEventType.UserBlocked
                : TelegramUserAuditEventType.UserUnblocked,
            context.Actor,
            context.Target,
            updated,
            action,
            cancellationToken);
        return new(TelegramUserManagementStatus.Success, updated);
    }

    private async Task<MutationContext> ResolveMutationAsync(
        long targetUserId,
        long actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _userStore.GetByIdAsync(actorUserId, cancellationToken);
        if (actor is not { Role: TelegramUserRole.Owner, IsEnabled: true, IsBlocked: false })
        {
            await AppendDeniedAuditAsync(actor, targetUserId, "unsupported", "insufficient_permissions", cancellationToken);
            return new(actor, null, new(TelegramUserManagementStatus.AccessDenied));
        }

        var target = await _userStore.GetByIdAsync(targetUserId, cancellationToken);
        return target is null
            ? new(actor, null, new(TelegramUserManagementStatus.NotFound))
            : new(actor, target, null);
    }

    private async Task<bool> IsLastOwnerAsync(CancellationToken cancellationToken)
    {
        var overview = await _userStore.GetUserOverviewAsync(cancellationToken);
        return overview.CountsByRole.GetValueOrDefault(TelegramUserRole.Owner) <= 1;
    }

    private async Task<TelegramUserManagementResult> DeniedAsync(
        TelegramUserManagementStatus status,
        TelegramUserSnapshot? actor,
        TelegramUserSnapshot? target,
        string action,
        string reason,
        CancellationToken cancellationToken)
    {
        await AppendDeniedAuditAsync(actor, target?.Id, action, reason, cancellationToken);
        return new(status, target);
    }

    private Task AppendAuditAsync(
        TelegramUserAuditEventType eventType,
        TelegramUserSnapshot actor,
        TelegramUserSnapshot before,
        TelegramUserSnapshot? after,
        string action,
        CancellationToken cancellationToken) =>
        _auditService?.AppendSafeAsync(
            new TelegramUserAuditEventCreate(
                eventType,
                actor.Id,
                before.Id,
                before.Role,
                after?.Role,
                before.IsEnabled,
                after?.IsEnabled,
                before.IsBlocked,
                after?.IsBlocked,
                true,
                null,
                JsonSerializer.Serialize(new { action }),
                DateTimeOffset.UtcNow),
            cancellationToken) ?? Task.CompletedTask;

    private Task AppendDeniedAuditAsync(
        TelegramUserSnapshot? actor,
        long? targetId,
        string action,
        string reason,
        CancellationToken cancellationToken) =>
        _auditService?.AppendSafeAsync(
            new TelegramUserAuditEventCreate(
                TelegramUserAuditEventType.UserActionDenied,
                actor?.Id,
                targetId,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                null,
                JsonSerializer.Serialize(new { action, reason }),
                DateTimeOffset.UtcNow),
            cancellationToken) ?? Task.CompletedTask;

    private sealed record MutationContext(
        TelegramUserSnapshot? Actor,
        TelegramUserSnapshot? Target,
        TelegramUserManagementResult? Result);
}
