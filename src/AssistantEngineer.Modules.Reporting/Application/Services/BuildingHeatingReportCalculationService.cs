using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Calculations.Application.Facades;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class BuildingHeatingReportCalculationService
{
    private readonly ILoadCalculationsFacade _loadCalculations;

    public BuildingHeatingReportCalculationService(
        ILoadCalculationsFacade loadCalculations)
    {
        _loadCalculations = loadCalculations;
    }

    public Task<Result<BuildingHeatingLoadResult>> CalculateBuildingHeatingLoadAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken = default) =>
        _loadCalculations.CalculateBuildingHeatingLoadAsync(
            buildingId,
            method,
            cancellationToken);
}