using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public sealed class TelegramUserRolePolicyTests
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
    [InlineData(TelegramUserRole.Owner, "Владелец")]
    [InlineData(TelegramUserRole.Admin, "Администратор")]
    [InlineData(TelegramUserRole.Engineer, "Сервис-инженер")]
    [InlineData(TelegramUserRole.Installer, "Монтажник")]
    [InlineData(TelegramUserRole.Consumer, "Клиент")]
    public void RoleLabelsAreRussian(TelegramUserRole role, string expected)
    {
        Assert.Equal(expected, TelegramUserRolePolicy.DisplayName(role));
    }
}
