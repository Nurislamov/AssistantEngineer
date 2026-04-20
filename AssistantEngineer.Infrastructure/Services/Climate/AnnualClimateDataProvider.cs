using AssistantEngineer.Application.Abstractions;
using AssistantEngineer.Application.Abstractions.Repositories;
using AssistantEngineer.Domain.Models.Climate;

namespace AssistantEngineer.Infrastructure.Services.Climate;

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