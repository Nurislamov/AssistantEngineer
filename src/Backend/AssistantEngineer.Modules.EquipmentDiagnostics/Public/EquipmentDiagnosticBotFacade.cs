using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Public;

public sealed class EquipmentDiagnosticBotFacade : IEquipmentDiagnosticBotFacade
{
    private readonly IEquipmentDiagnosticBotService _service;

    public EquipmentDiagnosticBotFacade(IEquipmentDiagnosticBotService service)
    {
        _service = service;
    }

    public Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
        EquipmentDiagnosticBotRequest request,
        CancellationToken cancellationToken = default) =>
        _service.DiagnoseAsync(request, cancellationToken);
}
