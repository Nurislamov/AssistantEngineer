using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

public sealed class EquipmentDiagnosticBotService : IEquipmentDiagnosticBotService
{
    private readonly IEquipmentDiagnosticCore _core;

    public EquipmentDiagnosticBotService(IEquipmentDiagnosticCore core)
    {
        _core = core;
    }

    public async Task<EquipmentDiagnosticBotResponse> DiagnoseAsync(
        EquipmentDiagnosticBotRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _core.DiagnoseAsync(
            EquipmentDiagnosticBotCompatibilityMapper.ToCoreRequest(request),
            cancellationToken);

        return EquipmentDiagnosticBotCompatibilityMapper.ToBotResponse(result);
    }
}
