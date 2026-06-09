using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Public;

public interface IEquipmentDiagnosticBotFacade
{
    Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
        EquipmentDiagnosticBotRequest request,
        CancellationToken cancellationToken = default);
}
