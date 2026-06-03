using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;

public interface IEquipmentDiagnosticsService
{
    Task<IReadOnlyList<EquipmentErrorCodeSummaryDto>> SearchErrorCodesAsync(
        SearchEquipmentErrorCodesQuery query,
        CancellationToken cancellationToken);

    Task<EquipmentDiagnosticCaseDto?> GetDiagnosticCaseAsync(
        string manufacturer,
        string errorCode,
        string? series,
        string? modelCode,
        CancellationToken cancellationToken);
}
