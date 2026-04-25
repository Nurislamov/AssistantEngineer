using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services;

internal sealed class EnergyPlusModelExportService
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
            request.RunName,
            cancellationToken);
    }
}
