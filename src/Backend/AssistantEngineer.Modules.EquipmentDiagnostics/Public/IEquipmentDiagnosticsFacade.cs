using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Public;

public interface IEquipmentDiagnosticsFacade
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

    Task<EquipmentDiagnosticsCatalogIndexDto> GetCatalogIndexAsync(
        CancellationToken cancellationToken = default);
}
