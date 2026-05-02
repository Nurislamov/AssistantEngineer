using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating;
using AssistantEngineer.Modules.Reporting.Application.Services;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Facades;

public sealed class BuildingHeatingReportsFacade : IBuildingHeatingReportsFacade
{
    private readonly BuildingHeatingReportDataService _reportDataService;

    internal BuildingHeatingReportsFacade(
        BuildingHeatingReportDataService reportDataService)
    {
        _reportDataService = reportDataService;
    }

    public Task<Result<BuildingHeatingReport>> BuildHeatingReportAsync(
        int buildingId,
        HeatingLoadCalculationMethodDto method,
        CancellationToken cancellationToken) =>
        _reportDataService.BuildHeatingReportAsync(
            buildingId,
            method,
            cancellationToken);
}