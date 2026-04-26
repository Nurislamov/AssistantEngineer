using AssistantEngineer.Modules.Buildings.Domain.Climate;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface IClimateDataRepository
{
    Task<ClimateData?> GetForClimateZoneAsync(
        int climateZoneId,
        int month,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetAvailableMonthsForClimateZoneAsync(
        int climateZoneId,
        CancellationToken cancellationToken = default);
}
