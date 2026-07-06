namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Diagnostics;

public interface IEquipmentDiagnosticCore
{
    Task<DiagnosticCoreResult> DiagnoseAsync(
        DiagnosticCoreRequest request,
        CancellationToken cancellationToken = default);
}
