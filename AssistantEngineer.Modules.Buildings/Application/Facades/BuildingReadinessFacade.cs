using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Buildings.Application.Services.Buildings;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Buildings.Application.Facades;

public sealed class BuildingReadinessFacade : IBuildingReadinessFacade
{
    private const int DefaultWeatherYear = 2020;

    private readonly BuildingCalculationReadinessService _readiness;

    public BuildingReadinessFacade(BuildingCalculationReadinessService readiness)
    {
        _readiness = readiness;
    }

    public Task<Result<BuildingCalculationReadinessReport>> CheckAsync(
        int buildingId,
        int? weatherYear,
        CancellationToken cancellationToken)
    {
        var effectiveWeatherYear = weatherYear ?? DefaultWeatherYear;
        return _readiness.CheckAsync(buildingId, effectiveWeatherYear, cancellationToken);
    }
}
