using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public interface IAnnualClimateDataFacade
{
    Task<Result<AnnualClimateDataImportResponse>> ImportFromEpwAsync(
        int climateZoneId,
        int year,
        Stream source,
        string sourceFileName,
        CancellationToken cancellationToken);
}
