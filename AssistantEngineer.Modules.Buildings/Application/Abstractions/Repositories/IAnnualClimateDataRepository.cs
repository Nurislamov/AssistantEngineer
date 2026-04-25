using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IAnnualClimateDataRepository
{
    Task<AnnualClimateData?> GetForClimateZoneAsync(
        int climateZoneId,
        int year,
        CancellationToken cancellationToken = default);

    Task ReplaceForClimateZoneAsync(
        AnnualClimateData annualClimateData,
        CancellationToken cancellationToken = default);
}
