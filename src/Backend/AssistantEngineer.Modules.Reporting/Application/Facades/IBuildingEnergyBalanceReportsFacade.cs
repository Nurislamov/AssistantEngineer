using AssistantEngineer.Modules.Calculations.Application.Contracts.Common;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Reporting.Application.Facades;

public interface IBuildingEnergyBalanceReportsFacade
{
    Task<Result<byte[]>> GenerateEnergyBalanceReportExcelAsync(
        int buildingId,
        CoolingLoadCalculationMethodDto coolingMethod,
        HeatingLoadCalculationMethodDto heatingMethod,
        CancellationToken cancellationToken);
}