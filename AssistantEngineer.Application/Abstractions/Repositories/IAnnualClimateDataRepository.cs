using AssistantEngineer.Domain.Models.Climate;

namespace AssistantEngineer.Application.Abstractions.Repositories;

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
