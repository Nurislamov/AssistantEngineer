namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

internal interface ITelegramUpdateHandler
{
    Task<EquipmentDiagnosticTelegramResponse?> TryHandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken);
}
