using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Localization;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics;

public sealed class EquipmentDiagnosticCore : IEquipmentDiagnosticCore
{
    private readonly EquipmentDiagnosticCoreEngine _engine;
    private readonly IErrorKnowledgeLocalizationSource _localizedKnowledge;

    public EquipmentDiagnosticCore(
        IEquipmentDiagnosticsService diagnosticsService,
        IErrorKnowledgeLocalizationSource localizedKnowledge)
    {
        _engine = new EquipmentDiagnosticCoreEngine(diagnosticsService, localizedKnowledge);
        _localizedKnowledge = localizedKnowledge;
    }

    public async Task<DiagnosticCoreResult> DiagnoseAsync(
        DiagnosticCoreRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var compatibilityResponse = await _engine.DiagnoseAsync(
            EquipmentDiagnosticBotCompatibilityMapper.ToBotRequest(request),
            cancellationToken);

        return EquipmentDiagnosticBotCompatibilityMapper.ToCoreResult(
            compatibilityResponse,
            _localizedKnowledge);
    }
}
