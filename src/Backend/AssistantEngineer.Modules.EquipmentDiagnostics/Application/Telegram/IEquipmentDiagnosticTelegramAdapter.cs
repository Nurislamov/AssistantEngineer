namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public interface IEquipmentDiagnosticTelegramAdapter
{
    Task<EquipmentDiagnosticTelegramResponse> HandleAsync(
        EquipmentDiagnosticTelegramUpdate update,
        CancellationToken cancellationToken = default);
}
