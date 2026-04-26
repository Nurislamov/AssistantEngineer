using AssistantEngineer.Modules.Buildings.Domain.Settings;

namespace AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;

public interface ICalculationPreferencesRepository
{
    Task<CalculationPreferences?> GetByProjectIdAsync(int projectId, CancellationToken cancellationToken = default);
}
