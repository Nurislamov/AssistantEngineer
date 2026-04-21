using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Persistence.Services.Climate;

internal sealed class AnnualClimateDataProvider : IAnnualClimateDataProvider
{
    private readonly IAnnualClimateDataRepository _repository;

    public AnnualClimateDataProvider(IAnnualClimateDataRepository repository) => _repository = repository;

    public async Task<AnnualClimateData?> GetForClimateZoneAsync(
        int climateZoneId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetForClimateZoneAsync(climateZoneId, year, cancellationToken);
    }
}