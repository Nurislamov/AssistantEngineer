using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Public;

public sealed class EquipmentDiagnosticsFacade : IEquipmentDiagnosticsFacade
{
    private readonly IEquipmentDiagnosticsService _service;

    public EquipmentDiagnosticsFacade(IEquipmentDiagnosticsService service)
    {
        _service = service;
    }

    public Task<IReadOnlyList<EquipmentErrorCodeSummaryDto>> SearchErrorCodesAsync(
        SearchEquipmentErrorCodesQuery query,
        CancellationToken cancellationToken) =>
        _service.SearchErrorCodesAsync(query, cancellationToken);

    public Task<EquipmentDiagnosticCaseDto?> GetDiagnosticCaseAsync(
        string manufacturer,
        string errorCode,
        string? series,
        string? modelCode,
        CancellationToken cancellationToken) =>
        _service.GetDiagnosticCaseAsync(manufacturer, errorCode, series, modelCode, cancellationToken);

    public Task<EquipmentDiagnosticsCatalogIndexDto> GetCatalogIndexAsync(
        CancellationToken cancellationToken = default) =>
        _service.GetCatalogIndexAsync(cancellationToken);
}
