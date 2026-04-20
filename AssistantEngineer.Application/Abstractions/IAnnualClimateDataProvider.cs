using AssistantEngineer.Domain.Models.Climate;

namespace AssistantEngineer.Application.Abstractions;

public interface IAnnualClimateDataProvider
{
    Task<AnnualClimateData?> GetForClimateZoneAsync(
        int climateZoneId,
        int year,
        CancellationToken cancellationToken = default);
}