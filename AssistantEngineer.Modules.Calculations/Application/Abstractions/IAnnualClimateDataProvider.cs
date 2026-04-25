using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions;

public interface IAnnualClimateDataProvider
{
    Task<AnnualClimateData?> GetForClimateZoneAsync(
        int climateZoneId,
        int year,
        CancellationToken cancellationToken = default);
}