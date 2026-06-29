namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Users;

public static class TelegramUserRolePolicy
{
    public static bool IsAdminRole(TelegramUserRole role) =>
        role is TelegramUserRole.Owner or TelegramUserRole.Admin;

    public static bool IsServiceEngineerRole(TelegramUserRole role) =>
        role == TelegramUserRole.Engineer;

    public static bool CanManageTelegramUsers(TelegramUserRole role) =>
        IsAdminRole(role);

    public static bool CanUseServiceQueue(TelegramUserRole role) =>
        IsAdminRole(role) || IsServiceEngineerRole(role);

    public static bool CanTakeServiceRequest(TelegramUserRole role) =>
        CanUseServiceQueue(role);

    public static bool CanAssignServiceRequest(TelegramUserRole role) =>
        IsAdminRole(role);

    public static bool CanReceivePrivateContact(TelegramUserRole role) =>
        CanUseServiceQueue(role);

    public static bool CanViewServiceRequestHistory(TelegramUserRole role) =>
        CanUseServiceQueue(role);

    public static bool CanViewTechnicalDiagnostics(TelegramUserRole role) =>
        role is TelegramUserRole.Owner or
            TelegramUserRole.Admin or
            TelegramUserRole.Engineer or
            TelegramUserRole.Installer;

    public static bool CanAccessDiagnosticManual(TelegramUserRole role) =>
        true;

    public static bool CanAccessTelegramLibrary(
        TelegramUserRole role,
        bool hasActiveGrant) =>
        role == TelegramUserRole.Owner ||
        hasActiveGrant && role is TelegramUserRole.Admin or TelegramUserRole.Engineer or TelegramUserRole.Installer;

    public static bool CanRequestTelegramLibraryAccess(TelegramUserRole role) =>
        role is TelegramUserRole.Admin or TelegramUserRole.Engineer or TelegramUserRole.Installer;

    public static bool CanManageTelegramLibrary(TelegramUserRole role) =>
        role == TelegramUserRole.Owner;

    public static bool HasAtLeastRole(
        TelegramUserRole actual,
        TelegramUserRole required) =>
        RoleRank(actual) <= RoleRank(required);

    private static int RoleRank(TelegramUserRole role) =>
        role switch
        {
            TelegramUserRole.Owner => 0,
            TelegramUserRole.Admin => 1,
            TelegramUserRole.Engineer => 2,
            TelegramUserRole.Installer => 3,
            TelegramUserRole.Consumer => 4,
            _ => 5
        };

    public static string DisplayName(TelegramUserRole role) =>
        role switch
        {
            TelegramUserRole.Owner => "Владелец",
            TelegramUserRole.Admin => "Администратор",
            TelegramUserRole.Engineer => "Сервис-инженер",
            TelegramUserRole.Installer => "Монтажник",
            TelegramUserRole.Consumer => "Клиент",
            _ => role.ToString()
        };
}
