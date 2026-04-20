using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Contracts.Benchmarks;
using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Application.Services.Benchmarks;

public sealed class EnergyPlusModelExportService
{
    private readonly IBuildingRepository _buildings;
    private readonly IEnergyPlusModelExporter _energyPlusModelExporter;

    public EnergyPlusModelExportService(
        IBuildingRepository buildings,
        IEnergyPlusModelExporter energyPlusModelExporter)
    {
        _buildings = buildings;
        _energyPlusModelExporter = energyPlusModelExporter;
    }

    public async Task<Result<EnergyPlusModelExportResult>> ExportBuildingModelAsync(
        int buildingId,
        EnergyPlusModelExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var building = await _buildings.GetForCalculationAsync(buildingId, cancellationToken);
        if (building is null)
            return Result<EnergyPlusModelExportResult>.NotFound($"Building with id {buildingId} not found.");

        return await _energyPlusModelExporter.ExportAsync(
            building,
            request.OutputPath,
            cancellationToken);
    }
}
