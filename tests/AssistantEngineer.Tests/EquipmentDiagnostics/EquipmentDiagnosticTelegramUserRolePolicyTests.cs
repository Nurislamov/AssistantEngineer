using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class EquipmentDiagnosticTelegramUserRolePolicyTests
{
    [Theory]
    [InlineData(TelegramUserRole.Owner, true, true, true)]
    [InlineData(TelegramUserRole.Admin, true, true, true)]
    [InlineData(TelegramUserRole.Engineer, false, true, true)]
    [InlineData(TelegramUserRole.Installer, false, false, true)]
    [InlineData(TelegramUserRole.Consumer, false, false, false)]
    public void PermissionMatrixIsCentralized(
        TelegramUserRole role,
        bool canManageUsers,
        bool canUseServiceQueue,
        bool canViewTechnicalDiagnostics)
    {
        Assert.Equal(canManageUsers, TelegramUserRolePolicy.CanManageTelegramUsers(role));
        Assert.Equal(canUseServiceQueue, TelegramUserRolePolicy.CanUseServiceQueue(role));
        Assert.Equal(canUseServiceQueue, TelegramUserRolePolicy.CanTakeServiceRequest(role));
        Assert.Equal(canUseServiceQueue, TelegramUserRolePolicy.CanReceivePrivateContact(role));
        Assert.Equal(canUseServiceQueue, TelegramUserRolePolicy.CanViewServiceRequestHistory(role));
        Assert.Equal(canViewTechnicalDiagnostics, TelegramUserRolePolicy.CanViewTechnicalDiagnostics(role));
    }

    [Theory]
    [InlineData(TelegramUserRole.Owner, "\u0412\u043B\u0430\u0434\u0435\u043B\u0435\u0446")]
    [InlineData(TelegramUserRole.Admin, "\u0410\u0434\u043C\u0438\u043D\u0438\u0441\u0442\u0440\u0430\u0442\u043E\u0440")]
    [InlineData(TelegramUserRole.Engineer, "\u0421\u0435\u0440\u0432\u0438\u0441-\u0438\u043D\u0436\u0435\u043D\u0435\u0440")]
    [InlineData(TelegramUserRole.Installer, "\u041C\u043E\u043D\u0442\u0430\u0436\u043D\u0438\u043A")]
    [InlineData(TelegramUserRole.Consumer, "\u041A\u043B\u0438\u0435\u043D\u0442")]
    public void RoleLabelsAreRussian(TelegramUserRole role, string expected)
    {
        Assert.Equal(expected, TelegramUserRolePolicy.DisplayName(role));
    }

    [Theory]
    [InlineData(TelegramUserRole.Owner, false, true)]
    [InlineData(TelegramUserRole.Admin, false, false)]
    [InlineData(TelegramUserRole.Admin, true, true)]
    [InlineData(TelegramUserRole.Engineer, false, false)]
    [InlineData(TelegramUserRole.Engineer, true, true)]
    [InlineData(TelegramUserRole.Installer, false, false)]
    [InlineData(TelegramUserRole.Installer, true, true)]
    [InlineData(TelegramUserRole.Consumer, true, false)]
    public void TelegramLibraryAccessIsControlledByExplicitGrantForTechnicalRoles(
        TelegramUserRole role,
        bool hasActiveGrant,
        bool expected)
    {
        Assert.Equal(expected, TelegramUserRolePolicy.CanAccessTelegramLibrary(role, hasActiveGrant));
    }
}
