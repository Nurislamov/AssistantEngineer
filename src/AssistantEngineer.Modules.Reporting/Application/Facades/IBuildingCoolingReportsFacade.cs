using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Facades;

public interface IBuildingCoolingReportsFacade
{
    Task<Result<BuildingCoolingReport>> BuildCoolingReportAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken);

    Task<Result<byte[]>> GenerateCoolingReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto method,
        string? systemType,
        string? unitType,
        CancellationToken cancellationToken);
}