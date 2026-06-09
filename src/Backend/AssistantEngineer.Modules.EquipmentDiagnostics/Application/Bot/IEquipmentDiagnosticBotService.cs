namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public interface IEquipmentDiagnosticBotService
{
    Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
        EquipmentDiagnosticBotRequest request,
        CancellationToken cancellationToken = default);
}
