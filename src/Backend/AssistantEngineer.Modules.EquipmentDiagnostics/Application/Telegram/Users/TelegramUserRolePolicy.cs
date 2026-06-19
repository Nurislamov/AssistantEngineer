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
