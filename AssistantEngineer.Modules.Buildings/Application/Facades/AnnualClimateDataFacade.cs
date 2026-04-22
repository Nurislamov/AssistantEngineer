using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Climate;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public sealed class AnnualClimateDataFacade : IAnnualClimateDataFacade
{
    private readonly EpwAnnualClimateDataImportService _epwImport;

    public AnnualClimateDataFacade(EpwAnnualClimateDataImportService epwImport)
    {
        _epwImport = epwImport;
    }

    public Task<Result<AnnualClimateDataImportResponse>> ImportFromEpwAsync(
        int climateZoneId,
        int year,
        Stream source,
        string sourceFileName,
        CancellationToken cancellationToken) =>
        _epwImport.ImportAsync(
            climateZoneId,
            year,
            source,
            sourceFileName,
            cancellationToken);
}
